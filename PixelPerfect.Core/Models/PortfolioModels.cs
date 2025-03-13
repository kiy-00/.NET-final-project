// Models/PortfolioModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PixelPerfect.Core.Models
{
    // 作品集DTO基类
    public abstract class PortfolioBaseDto
    {
        public int PortfolioId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CoverImageUrl { get; set; } // 新增：封面图片URL
        public string CoverThumbnailUrl { get; set; } // 新增：封面缩略图URL
        public List<PortfolioItemDto> Items { get; set; } = new List<PortfolioItemDto>();
    }

    // 摄影师作品集DTO
    public class PhotographerPortfolioDto : PortfolioBaseDto
    {
        public int PhotographerId { get; set; }
        public string PhotographerName { get; set; }
    }

    // 修图师作品集DTO
    public class RetoucherPortfolioDto : PortfolioBaseDto
    {
        public int RetoucherId { get; set; }
        public string RetoucherName { get; set; }
    }

    // 作品项DTO
    public class PortfolioItemDto
    {
        public int ItemId { get; set; }
        public int PortfolioId { get; set; }
        // 新增：作品集类型
        public string PortfolioType { get; set; }
        public string ImagePath { get; set; }
        public string ImageUrl { get; set; } // 图片完整访问URL
        public string ThumbnailUrl { get; set; } // 缩略图URL
        public string Title { get; set; }
        public string Description { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBeforeImage { get; set; }
        public int? AfterImageId { get; set; }
        public PortfolioItemDto AfterImage { get; set; }

        // 新增：标识是否为作品集封面
        public bool IsPortfolioCover { get; set; }

        // 新增：用于修图前后对比的字段
        public string BeforeImageUrl { get; set; }
        public string BeforeThumbnailUrl { get; set; }
        public string AfterImageUrl { get; set; }
        public string AfterThumbnailUrl { get; set; }
    }

    // 创建作品集请求基类
    public abstract class CreatePortfolioBaseRequest
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public string Category { get; set; }

        public bool IsPublic { get; set; } = true;
    }

    // 创建摄影师作品集请求
    public class CreatePhotographerPortfolioRequest : CreatePortfolioBaseRequest
    {
    }

    // 创建修图师作品集请求
    public class CreateRetoucherPortfolioRequest : CreatePortfolioBaseRequest
    {
    }

    // 更新作品集请求
    public class UpdatePortfolioRequest
    {
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public string Category { get; set; }

        public bool? IsPublic { get; set; }
    }

    // 上传作品项请求
    public class UploadPortfolioItemRequest
    {
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsBeforeImage { get; set; } = false;

        public int? AfterImageId { get; set; }

        // 新增：文件路径字段（用于内部服务调用）
        public string FilePath { get; set; }

        // 新增：元数据字段（用于内部服务调用）
        public string Metadata { get; set; }

        // 新增：标识是否为作品集封面
        public bool IsPortfolioCover { get; set; } = false;
    }

    // 新增：上传修图师作品项请求（支持前后对比）
    public class UploadRetoucherPortfolioItemRequest
    {
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // 注意：前端表单中需要包含 IFormFile afterImage 和可选的 IFormFile beforeImage
    }

    // 新增：批量上传作品项请求
    public class BatchPortfolioItemUploadRequest
    {
        [StringLength(500)]
        public string Description { get; set; }

        // 注意：前端表单中会包含多个 IFormFile 文件
    }

    // 更新作品项请求
    public class UpdatePortfolioItemRequest
    {
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool? IsBeforeImage { get; set; }

        public int? AfterImageId { get; set; }

        // 注意：通常不允许通过更新请求改变作品项的类型
        // 但是如果确实需要，可以添加以下字段
        // public string PortfolioType { get; set; }
    }

    // 作品集搜索参数
    public class PortfolioSearchParams
    {
        public string Keyword { get; set; }
        public string Category { get; set; }
        public bool? IsPublic { get; set; } = true;
    }
}