using PixelPerfect.Models;

namespace PixelPerfect.Services
{
    public interface INotificationService
    {
        // 获取通知
        Task<NotificationDto> GetNotificationByIdAsync(int notificationId);
        Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId, bool? isRead = null);
        Task<List<NotificationDto>> SearchNotificationsAsync(NotificationSearchParams searchParams);

        // 创建通知
        Task<NotificationDto> CreateNotificationAsync(NotificationCreateRequest request);

        // 批量创建通知（例如系统公告）
        Task<int> CreateNotificationsForAllUsersAsync(string type, string title, string content);

        // 更新通知
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);

        // 删除通知
        Task<bool> DeleteNotificationAsync(int notificationId);
        Task<bool> DeleteAllNotificationsAsync(int userId);

        // 权限检查
        Task<bool> IsUserNotificationAsync(int notificationId, int userId);

        // 获取未读通知数量
        Task<int> GetUnreadCountAsync(int userId);
    }
}