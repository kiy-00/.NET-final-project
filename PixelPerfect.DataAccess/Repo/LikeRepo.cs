using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.DataAccess.Repos
{
    public class LikeRepo
    {
        private readonly PhotoBookingDbContext _context;

        public LikeRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Like> GetByIdAsync(int likeId)
        {
            return await _context.Likes
                .Include(l => l.User)
                .Include(l => l.Post)
                .FirstOrDefaultAsync(l => l.LikeId == likeId);
        }

        public async Task<Like> GetByUserAndPostAsync(int userId, int postId)
        {
            return await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
        }

        public async Task<List<Like>> GetByPostIdAsync(int postId)
        {
            return await _context.Likes
                .Include(l => l.User)
                .Where(l => l.PostId == postId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Like>> GetByUserIdAsync(int userId)
        {
            return await _context.Likes
                .Include(l => l.Post)
                    .ThenInclude(p => p.User)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCountByPostIdAsync(int postId)
        {
            return await _context.Likes
                .Where(l => l.PostId == postId)
                .CountAsync();
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            return await _context.Likes
                .Where(l => l.UserId == userId)
                .CountAsync();
        }

        public async Task<Like> CreateAsync(Like like)
        {
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return like;
        }

        public async Task<bool> DeleteAsync(int likeId)
        {
            var like = await _context.Likes.FindAsync(likeId);
            if (like == null) return false;

            _context.Likes.Remove(like);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteByUserAndPostAsync(int userId, int postId)
        {
            var like = await GetByUserAndPostAsync(userId, postId);
            if (like == null) return false;

            _context.Likes.Remove(like);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}