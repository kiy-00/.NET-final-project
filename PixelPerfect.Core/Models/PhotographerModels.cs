using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Core.Models
{
    // 摄影师DTO
    public class PhotographerDto
    {
        public int PhotographerId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }
        public string Experience { get; set; }
        public string EquipmentInfo { get; set; }
        public string Location { get; set; }
        public decimal? PriceRangeMin { get; set; }
        public decimal? PriceRangeMax { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    // 创建摄影师档案请求
    public class PhotographerCreateRequest
    {
        [StringLength(1000)]
        public string Bio { get; set; }

        [StringLength(1000)]
        public string Experience { get; set; }

        [StringLength(500)]
        public string EquipmentInfo { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        [Range(0, 100000)]
        public decimal? PriceRangeMin { get; set; }

        [Range(0, 100000)]
        public decimal? PriceRangeMax { get; set; }
    }

    // 更新摄影师档案请求
    public class PhotographerUpdateRequest
    {
        [StringLength(1000)]
        public string Bio { get; set; }

        [StringLength(1000)]
        public string Experience { get; set; }

        [StringLength(500)]
        public string EquipmentInfo { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        [Range(0, 100000)]
        public decimal? PriceRangeMin { get; set; }

        [Range(0, 100000)]
        public decimal? PriceRangeMax { get; set; }
    }

    // 搜索摄影师参数
    public class PhotographerSearchParams
    {
        public string Keyword { get; set; }
        public string Location { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool VerifiedOnly { get; set; } = true;
    }

    // 新的搜索参数类
    public class PhotographerSearchParamsV2
    {
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool VerifiedOnly { get; set; } = true;
        public string? Location { get; set; }
    }
}