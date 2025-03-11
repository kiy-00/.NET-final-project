using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Userrole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    public string RoleType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
