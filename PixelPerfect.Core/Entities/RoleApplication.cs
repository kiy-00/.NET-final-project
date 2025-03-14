using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class RoleApplication
{
    public int ApplicationId { get; set; }

    public int UserId { get; set; }

    public string RoleType { get; set; } = null!;

    public string Status { get; set; } = null!;

    /// <summary>
    /// 申请资料，包括资质证明等信息
    /// </summary>
    public string ApplicationData { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// 处理申请的管理员ID
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    /// <summary>
    /// 管理员反馈意见
    /// </summary>
    public string? Feedback { get; set; }

    public virtual User? ProcessedByUser { get; set; }

    public virtual User User { get; set; } = null!;
}
