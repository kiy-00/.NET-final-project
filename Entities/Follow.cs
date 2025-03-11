using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class Follow
{
    public int FollowId { get; set; }

    /// <summary>
    /// 关注者ID
    /// </summary>
    public int FollowerId { get; set; }

    /// <summary>
    /// 被关注者ID
    /// </summary>
    public int FollowedId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual User Followed { get; set; } = null!;

    public virtual User Follower { get; set; } = null!;
}
