using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;

namespace PixelPerfect.Services.Impl
{
    public class NotificationService : INotificationService
    {
        private PhotoBookingDbContext _context;
        private readonly NotificationRepo _notificationRepo;
        private readonly UserRepo _userRepo;

        public NotificationService(
           PhotoBookingDbContext context,
            NotificationRepo notificationRepo,
            UserRepo userRepo)
        {
            _context = context;
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
        }

        public async Task<NotificationDto> GetNotificationByIdAsync(int notificationId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            if (notification == null)
                return null;

            return MapToDto(notification);
        }

        public async Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId, bool? isRead = null)
        {
            var notifications = await _notificationRepo.GetByUserIdAsync(userId, isRead);
            return notifications.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationDto>> SearchNotificationsAsync(NotificationSearchParams searchParams)
        {
            var notifications = await _notificationRepo.SearchAsync(
                searchParams.UserId,
                searchParams.Type,
                searchParams.IsRead,
                searchParams.StartDate,
                searchParams.EndDate
            );

            return notifications.Select(MapToDto).ToList();
        }

        public async Task<NotificationDto> CreateNotificationAsync(NotificationCreateRequest request)
        {
            // 检查用户是否存在
            var user = await _userRepo.GetByIdAsync(request.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {request.UserId} not found.");

            // 创建通知
            var newNotification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            };

            var createdNotification = await _notificationRepo.CreateAsync(newNotification);
            return MapToDto(createdNotification);
        }

        public async Task<int> CreateNotificationsForAllUsersAsync(string type, string title, string content)
        {
            // 获取所有活跃用户
            var users = await _userRepo.GetAllAsync();

            // 为每个用户创建通知
            var notifications = users.Select(user => new Notification
            {
                UserId = user.UserId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            }).ToList();

            // 批量创建通知
            return await _notificationRepo.CreateBulkAsync(notifications);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            if (notification == null)
                throw new KeyNotFoundException($"Notification with ID {notificationId} not found.");

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            return await _notificationRepo.UpdateAsync(notification);
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _notificationRepo.GetByUserIdAsync(userId, false);
            if (!notifications.Any())
                return true;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            _context.Notifications.UpdateRange(notifications);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            return await _notificationRepo.DeleteAsync(notificationId);
        }

        public async Task<bool> DeleteAllNotificationsAsync(int userId)
        {
            var affected = await _notificationRepo.DeleteAllForUserAsync(userId);
            return affected > 0;
        }

        public async Task<bool> IsUserNotificationAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            if (notification == null)
                return false;

            return notification.UserId == userId;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepo.GetUnreadCountAsync(userId);
        }

        // 辅助方法 - 实体映射到DTO
        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Content = notification.Content,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }
    }
}