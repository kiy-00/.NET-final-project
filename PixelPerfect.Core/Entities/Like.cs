using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Like
{
    public int LikeId { get; set; }

    public int UserId { get; set; }

    public int PostId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Post Post { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
