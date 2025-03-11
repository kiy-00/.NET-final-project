using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Userrolesview
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Roles { get; set; }
}
