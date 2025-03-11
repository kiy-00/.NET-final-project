using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Models
{
    // 预约详情DTO
    public class BookingDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int PhotographerId { get; set; }
        public string PhotographerName { get; set; }
        public DateTime BookingDate { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string Requirements { get; set; }
        public int PhotoCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsPublic { get; set; }
        public List<BookingServiceDto> Services { get; set; } = new List<BookingServiceDto>();
    }

    // 预约服务DTO
    public class BookingServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }

    // 创建预约请求
    public class BookingCreateRequest
    {
        public int PhotographerId { get; set; }
        public DateTime BookingDate { get; set; }
        public string Location { get; set; }
        public string Requirements { get; set; }
        public int PhotoCount { get; set; }
        public List<BookingServiceRequest> Services { get; set; } = new List<BookingServiceRequest>();
    }

    // 预约服务请求
    public class BookingServiceRequest
    {
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }

    // 更新预约状态请求
    public class BookingStatusUpdateRequest
    {
        public string Status { get; set; }
    }

    // 更新预约金额请求
    public class BookingAmountUpdateRequest
    {
        public decimal FinalAmount { get; set; }
    }

    // 预约交付成片请求
    public class BookingDeliverRequest
    {
        public bool IsPublic { get; set; }
    }

    // 预约查询参数
    public class BookingSearchParams
    {
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? PhotographerId { get; set; }
        public int? UserId { get; set; }
    }
}