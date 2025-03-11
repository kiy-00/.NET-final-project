using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class UserFollowersView
{
    /// <summary>
    /// 被关注者ID
    /// </summary>
    public int UserId { get; set; }

    public long FollowerCount { get; set; }

    public string? Followers { get; set; }
}
