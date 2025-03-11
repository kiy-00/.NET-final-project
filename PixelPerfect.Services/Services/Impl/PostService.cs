using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class PostService : IPostService
    {
        private readonly PhotoBookingDbContext _context;
        private readonly PostRepo _postRepo;
        private readonly LikeRepo _likeRepo;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly string _uploadDirectory;

        public PostService(
            PhotoBookingDbContext context,
            PostRepo postRepo,
            LikeRepo likeRepo,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _postRepo = postRepo;
            _likeRepo = likeRepo;
            _hostEnvironment = hostEnvironment;
            _uploadDirectory = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "posts");

            // 确保目录存在
            if (!Directory.Exists(_uploadDirectory))
                Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<PostDto> GetPostByIdAsync(int postId, int? currentUserId = null)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                return null;

            return await MapToDto(post, currentUserId);
        }

        public async Task<PostDetailDto> GetPostDetailByIdAsync(int postId, int? currentUserId = null)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                return null;

            var postDto = await MapToDto(post, currentUserId) as PostDetailDto;

            // 添加点赞信息
            var likes = await _likeRepo.GetByPostIdAsync(postId);
            postDto.Likes = likes.Select(l => new LikeDto
            {
                LikeId = l.LikeId,
                UserId = l.UserId,
                Username = l.User.Username,
                PostId = l.PostId,
                CreatedAt = l.CreatedAt
            }).ToList();

            return postDto;
        }

        public async Task<PagedResult<PostDto>> GetPostsByUserIdAsync(int userId, int page = 1, int pageSize = 10, int? currentUserId = null)
        {
            var skip = (page - 1) * pageSize;
            var posts = await _postRepo.GetByUserIdAsync(userId, skip, pageSize);
            var totalCount = await _postRepo.GetCountByUserIdAsync(userId);

            var postDtos = new List<PostDto>();
            foreach (var post in posts)
            {
                postDtos.Add(await MapToDto(post, currentUserId));
            }

            return new PagedResult<PostDto>
            {
                Items = postDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<PagedResult<PostDto>> SearchPostsAsync(PostSearchParams searchParams, int? currentUserId = null)
        {
            var (posts, totalCount) = await _postRepo.SearchAsync(
                searchParams.Keyword,
                searchParams.UserId,
                searchParams.IsApproved,
                searchParams.StartDate,
                searchParams.EndDate,
                searchParams.SortBy,
                searchParams.Descending,
                searchParams.Page,
                searchParams.PageSize
            );

            var postDtos = new List<PostDto>();
            foreach (var post in posts)
            {
                postDtos.Add(await MapToDto(post, currentUserId));
            }

            return new PagedResult<PostDto>
            {
                Items = postDtos,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)searchParams.PageSize)
            };
        }

        public async Task<PostDto> CreatePostAsync(int userId, PostCreateRequest request, IFormFile image = null)
        {
            var post = new Post
            {
                UserId = userId,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false // 默认需要审核
            };

            // 如果有图片，上传并保存路径
            if (image != null)
            {
                post.ImagePath = await SavePostImageAsync(image);
            }

            var createdPost = await _postRepo.CreateAsync(post);
            return await MapToDto(createdPost);
        }

        public async Task<PostDto> UpdatePostAsync(int postId, PostUpdateRequest request)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {postId} not found.");

            // 更新帖子
            if (!string.IsNullOrEmpty(request.Title))
                post.Title = request.Title;

            if (!string.IsNullOrEmpty(request.Content))
                post.Content = request.Content;

            // 更新后需要重新审核
            post.IsApproved = false;
            post.ApprovedAt = null;
            post.ApprovedByUserId = null;

            await _postRepo.UpdateAsync(post);
            return await MapToDto(post);
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                return false;

            // 删除相关的图片文件
            if (!string.IsNullOrEmpty(post.ImagePath))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, post.ImagePath.TrimStart('/'));
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }

            return await _postRepo.DeleteAsync(postId);
        }

        public async Task<string> UploadPostImageAsync(int postId, IFormFile image)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {postId} not found.");

            // 如果已经有图片，先删除
            if (!string.IsNullOrEmpty(post.ImagePath))
            {
                var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, post.ImagePath.TrimStart('/'));
                if (File.Exists(oldImagePath))
                {
                    File.Delete(oldImagePath);
                }
            }

            // 上传新图片
            var imagePath = await SavePostImageAsync(image);

            // 更新帖子
            post.ImagePath = imagePath;
            await _postRepo.UpdateAsync(post);

            return imagePath;
        }

        public async Task<PostDto> ApprovePostAsync(int postId, bool isApproved, int adminUserId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {postId} not found.");

            post.IsApproved = isApproved;
            if (isApproved)
            {
                post.ApprovedAt = DateTime.UtcNow;
                post.ApprovedByUserId = adminUserId;
            }
            else
            {
                post.ApprovedAt = null;
                post.ApprovedByUserId = null;
            }

            await _postRepo.UpdateAsync(post);
            return await MapToDto(post);
        }

        public async Task<PagedResult<PostDto>> GetPendingPostsAsync(int page = 1, int pageSize = 10)
        {
            var skip = (page - 1) * pageSize;
            var posts = await _postRepo.GetPendingPostsAsync(skip, pageSize);
            var totalCount = await _postRepo.GetPendingPostsCountAsync();

            var postDtos = new List<PostDto>();
            foreach (var post in posts)
            {
                postDtos.Add(await MapToDto(post));
            }

            return new PagedResult<PostDto>
            {
                Items = postDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<bool> IsPostOwnerAsync(int postId, int userId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                return false;

            return post.UserId == userId;
        }

        public async Task<int> GetPostsCountByUserIdAsync(int userId)
        {
            return await _postRepo.GetCountByUserIdAsync(userId);
        }

        public async Task<int> GetPendingPostsCountAsync()
        {
            return await _postRepo.GetPendingPostsCountAsync();
        }

        // 辅助方法 - 保存帖子图片
        private async Task<string> SavePostImageAsync(IFormFile image)
        {
            // 验证文件类型
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Only jpg, jpeg, png and gif files are allowed.");

            // 创建唯一文件名
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/uploads/posts/{uniqueFileName}";
        }

        // 辅助方法 - 实体映射到DTO
        private async Task<PostDto> MapToDto(Post post, int? currentUserId = null)
        {
            var likesCount = await _likeRepo.GetCountByPostIdAsync(post.PostId);
            bool isLiked = false;

            if (currentUserId.HasValue)
            {
                var like = await _likeRepo.GetByUserAndPostAsync(currentUserId.Value, post.PostId);
                isLiked = like != null;
            }

            var dto = new PostDetailDto
            {
                PostId = post.PostId,
                UserId = post.UserId,
                Username = post.User?.Username,
                Title = post.Title,
                Content = post.Content,
                ImagePath = post.ImagePath,
                CreatedAt = post.CreatedAt,
                IsApproved = post.IsApproved,
                ApprovedAt = post.ApprovedAt,
                LikesCount = likesCount,
                IsLikedByCurrentUser = isLiked
            };

            return dto;
        }
    }
}