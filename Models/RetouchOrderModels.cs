using System;
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Models
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

    // 完成修图订单请求
    public class RetouchOrderCompleteRequest
    {
        [StringLength(500)]
        public string Comment { get; set; }
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