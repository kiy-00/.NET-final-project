using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class UserFollowingView
{
    /// <summary>
    /// 关注者ID
    /// </summary>
    public int UserId { get; set; }

    public long FollowingCount { get; set; }

    public string? Following { get; set; }
}
