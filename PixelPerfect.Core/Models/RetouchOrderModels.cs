using System;
using System.ComponentModel.DataAnnotations;
namespace PixelPerfect.Core.Models
{
    // 修图订单DTO
    public class RetouchOrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int RetoucherId { get; set; }
        public string RetoucherName { get; set; }
        public int PhotoId { get; set; }
        public string PhotoTitle { get; set; }
        public string PhotoPath { get; set; }
        // 新增字段
        public int? RetouchedPhotoId { get; set; }
        public string RetouchedPhotoTitle { get; set; }
        public string RetouchedPhotoPath { get; set; }
        public string Status { get; set; }
        public string Requirements { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    // 创建修图订单请求
    public class RetouchOrderCreateRequest
    {
        [Required]
        public int RetoucherId { get; set; }
        [Required]
        public int PhotoId { get; set; }
        [StringLength(2000)]
        public string Requirements { get; set; }
    }

    // 更新修图订单状态请求
    public class RetouchOrderStatusUpdateRequest
    {
        [Required]
        public string Status { get; set; }
    }

    // 完成修图订单请求 - 简化为只上传照片
    public class RetouchOrderCompleteRequest
    {
        // 保留原有注释字段以便客户添加备注
        [StringLength(500)]
        public string Comment { get; set; }

        // 照片相关信息将通过FormFile上传，不在此DTO中定义
    }

    // 修图订单搜索参数
    public class RetouchOrderSearchParams
    {
        public int? UserId { get; set; }
        public int? RetoucherId { get; set; }
        public int? PhotoId { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}