// Models/PortfolioModels.cs
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Models
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
        public string ImagePath { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBeforeImage { get; set; }
        public int? AfterImageId { get; set; }
        public PortfolioItemDto AfterImage { get; set; }
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
    }

    // 作品集搜索参数
    public class PortfolioSearchParams
    {
        public string Keyword { get; set; }
        public string Category { get; set; }
        public bool? IsPublic { get; set; } = true;
    }
}