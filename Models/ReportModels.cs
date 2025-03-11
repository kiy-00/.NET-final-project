using System;
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Models
{
    // 举报DTO
    public class ReportDto
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? HandledAt { get; set; }
        public int? HandledByUserId { get; set; }
        public string HandledByUsername { get; set; }
    }

    // 创建举报请求
    public class ReportCreateRequest
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 10)]
        public string Reason { get; set; }
    }

    // 处理举报请求
    public class ReportHandleRequest
    {
        [Required]
        public string Status { get; set; }  // "Approved", "Rejected", "Pending"

        [StringLength(500)]
        public string Comment { get; set; }
    }

    // 举报搜索参数
    public class ReportSearchParams
    {
        public int? UserId { get; set; }
        public int? PostId { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}