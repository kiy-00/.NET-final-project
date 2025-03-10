using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;

namespace PixelPerfect.Repos
{
    public class NotificationRepo
    {
        private PhotoBookingDbContext _context;

        public NotificationRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId, bool? isRead = null)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> SearchAsync(int? userId = null, string type = null,
                                                        bool? isRead = null, DateTime? startDate = null,
                                                        DateTime? endDate = null)
        {
            var query = _context.Notifications
                .Include(n => n.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedAt <= endDate.Value);

            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<int> CreateBulkAsync(List<Notification> notifications)
        {
            _context.Notifications.AddRange(notifications);
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            var notification = await GetByIdAsync(notificationId);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<int> DeleteAllForUserAsync(int userId)
        {
            var notifications = await GetByUserIdAsync(userId);
            if (!notifications.Any()) return 0;

            _context.Notifications.RemoveRange(notifications);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
    }
}