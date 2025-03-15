using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Core.Models
{
    // 修图师DTO
    public class RetoucherDto
    {
        public int RetoucherId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }
        public string Expertise { get; set; }
        public string Software { get; set; }
        public decimal? PricePerPhoto { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    // 创建修图师档案请求
    public class RetoucherCreateRequest
    {
        [StringLength(1000)]
        public string Bio { get; set; }

        [StringLength(1000)]
        public string Expertise { get; set; }

        [StringLength(500)]
        public string Software { get; set; }

        [Range(0, 100000)]
        public decimal? PricePerPhoto { get; set; }
    }

    // 更新修图师档案请求
    public class RetoucherUpdateRequest
    {
        [StringLength(1000)]
        public string Bio { get; set; }

        [StringLength(1000)]
        public string Expertise { get; set; }

        [StringLength(500)]
        public string Software { get; set; }

        [Range(0, 100000)]
        public decimal? PricePerPhoto { get; set; }
    }

    // 搜索修图师参数
    public class RetoucherSearchParams
    {
        public string Keyword { get; set; }
        public string Expertise { get; set; }
        public string Software { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool VerifiedOnly { get; set; } = true;
    }

    public class RetoucherSearchParamsV2
    {
        // 单一关键词，可搜索多个字段
        public string? Keyword { get; set; }

        // 价格区间（可选）
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // 是否只显示已验证修图师
        public bool VerifiedOnly { get; set; } = true;
    }
}