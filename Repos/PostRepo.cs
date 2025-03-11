using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;
using PixelPerfect.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Repos
{
    public class PostRepo
    {
        private readonly PhotoBookingDbContext _context;

        public PostRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Post> GetByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetByUserIdAsync(int userId, int skip = 0, int take = 10)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => p.UserId == userId && p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId && p.IsApproved)
                .CountAsync();
        }

        public async Task<List<Post>> GetPendingPostsAsync(int skip = 0, int take = 10)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => !p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetPendingPostsCountAsync()
        {
            return await _context.Posts
                .Where(p => !p.IsApproved)
                .CountAsync();
        }

        public async Task<(List<Post> Posts, int TotalCount)> SearchAsync(
            string keyword = null,
            int? userId = null,
            bool? isApproved = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortBy = "CreatedAt",
            bool descending = true,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .AsQueryable();

            // 应用筛选条件
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(keyword) ||
                    p.Content.ToLower().Contains(keyword));
            }

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (isApproved.HasValue)
                query = query.Where(p => p.IsApproved == isApproved.Value);
            else
                query = query.Where(p => p.IsApproved); // 默认只显示已批准的帖子

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            // 计算总数
            var totalCount = await query.CountAsync();

            // 应用排序和分页
            switch (sortBy.ToLower())
            {
                case "likescount":
                    query = descending
                        ? query.OrderByDescending(p => p.Likes.Count)
                        : query.OrderBy(p => p.Likes.Count);
                    break;
                case "createdat":
                default:
                    query = descending
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt);
                    break;
            }

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<Post> CreateAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            _context.Posts.Remove(post);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}