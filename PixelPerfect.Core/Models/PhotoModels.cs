using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Models
{
    // 照片DTO
    // PixelPerfect.Core/Models/PhotoModels.cs
    public class PhotoDto
    {
        public int PhotoId { get; set; }
        public int BookingId { get; set; }
        public string ImagePath { get; set; }
        public string ImageUrl { get; set; } // 添加可直接访问的URL
        public string ThumbnailUrl { get; set; } // 添加缩略图URL
        public string Title { get; set; }
        public string Description { get; set; }
        public string Metadata { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsPublic { get; set; }
        public bool ClientApproved { get; set; }
    }

    // 照片分组DTO
    public class PhotoCollectionDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string PhotographerName { get; set; }
        public string ClientName { get; set; }
        public DateTime BookingDate { get; set; }
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
    }

    // 上传照片请求
    public class PhotoUploadRequest
    {
        public int BookingId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
    }

    // 批量上传照片请求
    public class BatchPhotoUploadRequest
    {
        public int BookingId { get; set; }
        public bool IsPublic { get; set; }
    }

    // 更新照片请求
    public class PhotoUpdateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool? IsPublic { get; set; }
    }

    // 客户审批照片请求
    public class PhotoApprovalRequest
    {
        public bool ClientApproved { get; set; }
    }

    // 照片搜索参数
    public class PhotoSearchParams
    {
        public int? BookingId { get; set; }
        public int? UserId { get; set; }
        public int? PhotographerId { get; set; }
        public bool? IsPublic { get; set; }
        public bool? ClientApproved { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // 添加新的结果类型
    public class PhotoUploadResult
    {
        public int PhotoId { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class PortfolioItemUploadResult
    {
        public int PhotoId { get; set; }
        public string MainImageUrl { get; set; }
        public string MainThumbnailUrl { get; set; }
        public string? BeforeImageUrl { get; set; }
        public string? BeforeThumbnailUrl { get; set; }
    }

}