using PixelPerfect.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    public interface ILikeService
    {
        // 获取点赞信息
        Task<LikeDto> GetLikeByIdAsync(int likeId);
        Task<List<LikeDto>> GetLikesByPostIdAsync(int postId);
        Task<List<LikeDto>> GetLikesByUserIdAsync(int userId);

        // 点赞操作
        Task<LikeDto> LikePostAsync(int userId, int postId);
        Task<bool> UnlikePostAsync(int userId, int postId);

        // 检查是否已点赞
        Task<bool> HasUserLikedPostAsync(int userId, int postId);

        // 统计信息
        Task<int> GetLikesCountByPostIdAsync(int postId);
        Task<int> GetLikesCountByUserIdAsync(int userId);
    }
}