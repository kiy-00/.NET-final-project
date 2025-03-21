using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    public interface IPostService
    {
        // 获取帖子
        Task<PostDto> GetPostByIdAsync(int postId, int? currentUserId = null);
        Task<PostDetailDto> GetPostDetailByIdAsync(int postId, int? currentUserId = null);
        Task<PagedResult<PostDto>> GetPostsByUserIdAsync(int userId, int page = 1, int pageSize = 10, int? currentUserId = null);
        Task<PagedResult<PostDto>> SearchPostsAsync(PostSearchParams searchParams, int? currentUserId = null);

        // 创建和更新帖子
        Task<PostDto> CreatePostAsync(int userId, PostCreateRequest request, IFormFile image = null);
        Task<PostDto> UpdatePostAsync(int postId, PostUpdateRequest request);
        Task<bool> DeletePostAsync(int postId);

        // 帖子图片
        Task<string> UploadPostImageAsync(int postId, IFormFile image);

        // 管理员功能
        Task<PostDto> ApprovePostAsync(int postId, bool isApproved, int adminUserId);
        Task<PagedResult<PostDto>> GetPendingPostsAsync(int page = 1, int pageSize = 10);

        // 权限检查
        Task<bool> IsPostOwnerAsync(int postId, int userId);

        // 统计信息
        Task<int> GetPostsCountByUserIdAsync(int userId);
        Task<int> GetPendingPostsCountAsync();

        Task<List<PostDto>> GetLatestPostsAsync(int count, int? currentUserId);

        Task<PagedResult<PostDto>> SearchPostsV2Async(PostSearchParamsV2 searchParams, int? currentUserId = null);
    }
}