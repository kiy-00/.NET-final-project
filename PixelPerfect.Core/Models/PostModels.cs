using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Core.Models
{
    // 帖子DTO
    public class PostDto
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string UserAvatar { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

    // 帖子详情DTO (包含更多信息)
    public class PostDetailDto : PostDto
    {
        public List<LikeDto> Likes { get; set; } = new List<LikeDto>();
    }

    // 创建帖子请求
    public class PostCreateRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Content { get; set; }
    }

    // 更新帖子请求
    public class PostUpdateRequest
    {
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(2000, MinimumLength = 10)]
        public string Content { get; set; }
    }

    // 管理员审核帖子请求
    public class PostApproveRequest
    {
        [Required]
        public bool IsApproved { get; set; }
    }

    // 帖子搜索参数
    public class PostSearchParams
    {
        public string Keyword { get; set; }
        public int? UserId { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SortBy { get; set; } = "CreatedAt"; // CreatedAt, LikesCount
        public bool Descending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 分页结果
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}