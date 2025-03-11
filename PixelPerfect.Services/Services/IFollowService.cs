using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    public interface IFollowService
    {
        // 关注用户
        Task<bool> FollowUserAsync(int followerId, int followedId);

        // 取消关注
        Task<bool> UnfollowUserAsync(int followerId, int followedId);

        // 检查是否已关注
        Task<bool> IsFollowingAsync(int followerId, int followedId);

        // 获取用户的关注者
        Task<FollowListResponse> GetFollowersAsync(int userId, int pageNumber = 1, int pageSize = 20);

        // 获取用户关注的人
        Task<FollowListResponse> GetFollowingAsync(int userId, int pageNumber = 1, int pageSize = 20);

        // 获取关注统计
        Task<FollowStatsResponse> GetFollowStatsAsync(int userId);
    }
}