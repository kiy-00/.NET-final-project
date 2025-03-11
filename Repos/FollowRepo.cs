using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Repos
{
    public class FollowRepo
    {
        private readonly PhotoBookingDbContext _context;

        public FollowRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        // 获取关注关系
        public async Task<Follow?> GetFollowAsync(int followerId, int followedId)
        {
            return await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
        }

        // 创建关注关系
        public async Task<bool> CreateFollowAsync(int followerId, int followedId)
        {
            var follow = new Follow
            {
                FollowerId = followerId,
                FollowedId = followedId,
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.Follows.Add(follow);
            return await _context.SaveChangesAsync() > 0;
        }

        // 删除关注关系
        public async Task<bool> DeleteFollowAsync(int followerId, int followedId)
        {
            var follow = await GetFollowAsync(followerId, followedId);
            if (follow == null) return false;

            _context.Follows.Remove(follow);
            return await _context.SaveChangesAsync() > 0;
        }

        // 更新关注状态
        public async Task<bool> UpdateFollowStatusAsync(Follow follow, string status)
        {
            follow.Status = status;
            _context.Follows.Update(follow);
            return await _context.SaveChangesAsync() > 0;
        }

        // 获取用户的关注者
        public async Task<List<User>> GetFollowersAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Follows
                .Where(f => f.FollowedId == userId && f.Status == "Active")
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Follower)
                .ToListAsync();
        }

        // 获取用户关注的人
        public async Task<List<User>> GetFollowingAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Follows
                .Where(f => f.FollowerId == userId && f.Status == "Active")
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Followed)
                .ToListAsync();
        }

        // 获取关注者总数
        public async Task<int> GetFollowersCountAsync(int userId)
        {
            return await _context.Follows
                .CountAsync(f => f.FollowedId == userId && f.Status == "Active");
        }

        // 获取关注的人总数
        public async Task<int> GetFollowingCountAsync(int userId)
        {
            return await _context.Follows
                .CountAsync(f => f.FollowerId == userId && f.Status == "Active");
        }
    }
}