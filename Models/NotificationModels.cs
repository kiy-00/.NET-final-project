using System;
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Models
{
    // 通知DTO
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    // 创建通知请求
    public class NotificationCreateRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }
    }

    // 标记通知为已读请求
    public class NotificationMarkReadRequest
    {
        [Required]
        public bool IsRead { get; set; }
    }

    // 通知搜索参数
    public class NotificationSearchParams
    {
        public int? UserId { get; set; }
        public string Type { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}