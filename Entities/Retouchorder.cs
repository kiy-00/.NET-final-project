﻿using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class Retouchorder
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int RetoucherId { get; set; }

    public int PhotoId { get; set; }

    public string Status { get; set; } = null!;

    public string? Requirements { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Photo Photo { get; set; } = null!;

    public virtual Retoucher Retoucher { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
