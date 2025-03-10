using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Models;
using PixelPerfect.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // 获取通知详情
        [HttpGet("{notificationId}")]
        [Authorize]
        public async Task<IActionResult> GetNotificationById(int notificationId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var notification = await _notificationService.GetNotificationByIdAsync(notificationId);
                if (notification == null)
                    return NotFound(new { message = $"Notification with ID {notificationId} not found." });

                // 检查权限 - 只有通知所属用户和管理员可以查看
                if (notification.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving notification information." });
            }
        }

        // 获取用户的通知列表
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserNotifications([FromQuery] bool? isRead = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, isRead);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user notifications." });
            }
        }

        // 获取未读通知数量
        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving unread notification count." });
            }
        }

        // 标记通知为已读
        [HttpPut("{notificationId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限
                if (!await _notificationService.IsUserNotificationAsync(notificationId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _notificationService.MarkAsReadAsync(notificationId);
                if (success)
                    return Ok(new { message = "Notification marked as read." });
                else
                    return BadRequest(new { message = "Failed to mark notification as read." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking notification as read." });
            }
        }

        // 标记所有通知为已读
        [HttpPut("mark-all-read")]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var success = await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(new { message = "All notifications marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking all notifications as read." });
            }
        }

        // 删除通知
        [HttpDelete("{notificationId}")]
        [Authorize]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限
                if (!await _notificationService.IsUserNotificationAsync(notificationId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _notificationService.DeleteNotificationAsync(notificationId);
                if (success)
                    return Ok(new { message = "Notification deleted successfully." });
                else
                    return BadRequest(new { message = "Failed to delete notification." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting notification." });
            }
        }

        // 删除所有通知
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var success = await _notificationService.DeleteAllNotificationsAsync(userId);
                return Ok(new { message = "All notifications deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting all notifications." });
            }
        }

        // 创建通知（仅限管理员）
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var notification = await _notificationService.CreateNotificationAsync(request);
                return CreatedAtAction(nameof(GetNotificationById), new { notificationId = notification.NotificationId }, notification);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating notification." });
            }
        }

        // 创建全平台通知（仅限管理员）
        [HttpPost("system")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSystemNotification([FromBody] SystemNotificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var count = await _notificationService.CreateNotificationsForAllUsersAsync(
                    "System",
                    request.Title,
                    request.Content
                );

                return Ok(new { message = $"System notification sent to {count} users." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating system notification." });
            }
        }
    }

    // 系统通知请求（发送给所有用户）
    public class SystemNotificationRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
    }
}