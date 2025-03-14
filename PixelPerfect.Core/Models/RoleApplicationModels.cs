using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PixelPerfect.Core.Models
{
    // 角色申请创建请求
    public class CreateRoleApplicationRequest
    {
        [Required]
        public string RoleType { get; set; } = null!;

        [Required]
        public Dictionary<string, object> ApplicationData { get; set; } = null!;
    }

    // 角色申请处理请求
    public class ProcessRoleApplicationRequest
    {
        [Required]
        public string Status { get; set; } = null!; // Approved 或 Rejected

        public string? Feedback { get; set; }
    }

    // 角色申请响应
    public class RoleApplicationResponse
    {
        public int ApplicationID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; } = null!;
        public string RoleType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Dictionary<string, object> ApplicationData { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedByUserID { get; set; }
        public string? ProcessedByUsername { get; set; }
        public string? Feedback { get; set; }
    }

    // 分页查询参数
    public class RoleApplicationQueryParams
    {
        public string? Status { get; set; }
        public string? RoleType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // 分页响应
    public class PaginatedRoleApplicationResponse
    {
        public List<RoleApplicationResponse> Applications { get; set; } = new List<RoleApplicationResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}