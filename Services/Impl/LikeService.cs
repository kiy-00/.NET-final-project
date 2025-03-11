using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class LikeService : ILikeService
    {
        private readonly PhotoBookingDbContext _context;
        private readonly LikeRepo _likeRepo;
        private readonly PostRepo _postRepo;

        public LikeService(
            PhotoBookingDbContext context,
            LikeRepo likeRepo,
            PostRepo postRepo)
        {
            _context = context;
            _likeRepo = likeRepo;
            _postRepo = postRepo;
        }

        public async Task<LikeDto> GetLikeByIdAsync(int likeId)
        {
            var like = await _likeRepo.GetByIdAsync(likeId);
            if (like == null)
                return null;

            return MapToDto(like);
        }

        public async Task<List<LikeDto>> GetLikesByPostIdAsync(int postId)
        {
            var likes = await _likeRepo.GetByPostIdAsync(postId);
            return likes.Select(MapToDto).ToList();
        }

        public async Task<List<LikeDto>> GetLikesByUserIdAsync(int userId)
        {
            var likes = await _likeRepo.GetByUserIdAsync(userId);
            return likes.Select(MapToDto).ToList();
        }

        public async Task<LikeDto> LikePostAsync(int userId, int postId)
        {
            // 检查帖子是否存在
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {postId} not found.");

            // 检查帖子是否已审核通过
            if (!post.IsApproved)
                throw new InvalidOperationException("Cannot like a post that is not approved.");

            // 检查用户是否已点赞
            var existingLike = await _likeRepo.GetByUserAndPostAsync(userId, postId);
            if (existingLike != null)
                throw new InvalidOperationException("User has already liked this post.");

            // 创建点赞记录
            var like = new Like
            {
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };

            var createdLike = await _likeRepo.CreateAsync(like);
            return MapToDto(createdLike);
        }

        public async Task<bool> UnlikePostAsync(int userId, int postId)
        {
            return await _likeRepo.DeleteByUserAndPostAsync(userId, postId);
        }

        public async Task<bool> HasUserLikedPostAsync(int userId, int postId)
        {
            var like = await _likeRepo.GetByUserAndPostAsync(userId, postId);
            return like != null;
        }

        public async Task<int> GetLikesCountByPostIdAsync(int postId)
        {
            return await _likeRepo.GetCountByPostIdAsync(postId);
        }

        public async Task<int> GetLikesCountByUserIdAsync(int userId)
        {
            return await _likeRepo.GetCountByUserIdAsync(userId);
        }

        // 辅助方法 - 实体映射到DTO
        private LikeDto MapToDto(Like like)
        {
            return new LikeDto
            {
                LikeId = like.LikeId,
                UserId = like.UserId,
                Username = like.User?.Username,
                PostId = like.PostId,
                CreatedAt = like.CreatedAt
            };
        }
    }
}