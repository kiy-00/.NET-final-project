using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class FollowService : IFollowService
    {
        private readonly FollowRepo _followRepo;
        private readonly NotificationRepo _notificationRepo;

        public FollowService(FollowRepo followRepo, NotificationRepo notificationRepo)
        {
            _followRepo = followRepo;
            _notificationRepo = notificationRepo;
        }

        public async Task<bool> FollowUserAsync(int followerId, int followedId)
        {
            // 不能关注自己
            if (followerId == followedId)
                return false;

            // 检查是否已经关注
            var existingFollow = await _followRepo.GetFollowAsync(followerId, followedId);
            if (existingFollow != null)
            {
                // 如果之前取消了关注，可以重新激活
                if (existingFollow.Status != "Active")
                {
                    var result = await _followRepo.UpdateFollowStatusAsync(existingFollow, "Active");
                    if (result)
                    {
                        // 创建通知
                        await CreateFollowNotificationAsync(followerId, followedId);
                    }
                    return result;
                }
                return false; // 已经处于关注状态
            }

            // 创建新的关注关系
            var success = await _followRepo.CreateFollowAsync(followerId, followedId);

            if (success)
            {
                // 创建通知
                await CreateFollowNotificationAsync(followerId, followedId);
            }

            return success;
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followedId)
        {
            var follow = await _followRepo.GetFollowAsync(followerId, followedId);
            if (follow == null || follow.Status != "Active")
                return false;

            return await _followRepo.DeleteFollowAsync(followerId, followedId);
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followedId)
        {
            var follow = await _followRepo.GetFollowAsync(followerId, followedId);
            return follow != null && follow.Status == "Active";
        }

        public async Task<FollowListResponse> GetFollowersAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            var followers = await _followRepo.GetFollowersAsync(userId, pageNumber, pageSize);
            var totalCount = await _followRepo.GetFollowersCountAsync(userId);

            return new FollowListResponse
            {
                Users = followers.Select(u => new UserBriefInfo
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<FollowListResponse> GetFollowingAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            var following = await _followRepo.GetFollowingAsync(userId, pageNumber, pageSize);
            var totalCount = await _followRepo.GetFollowingCountAsync(userId);

            return new FollowListResponse
            {
                Users = following.Select(u => new UserBriefInfo
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<FollowStatsResponse> GetFollowStatsAsync(int userId)
        {
            var followersCount = await _followRepo.GetFollowersCountAsync(userId);
            var followingCount = await _followRepo.GetFollowingCountAsync(userId);

            return new FollowStatsResponse
            {
                FollowersCount = followersCount,
                FollowingCount = followingCount
            };
        }

        private async Task CreateFollowNotificationAsync(int followerId, int followedId)
        {
            // 创建一个关注通知
            var notification = new Notification
            {
                UserId = followedId,
                Title = "新的关注者",
                Content = $"有新用户关注了你",
                Type = "Follow",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepo.CreateAsync(notification);
        }
    }
}