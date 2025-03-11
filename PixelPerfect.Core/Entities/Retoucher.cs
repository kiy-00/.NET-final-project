using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Retoucher
{
    public int RetoucherId { get; set; }

    public int UserId { get; set; }

    public string? Bio { get; set; }

    public string? Expertise { get; set; }

    public string? Software { get; set; }

    public decimal? PricePerPhoto { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public virtual ICollection<Retoucherportfolio> Retoucherportfolios { get; set; } = new List<Retoucherportfolio>();

    public virtual ICollection<Retouchorder> Retouchorders { get; set; } = new List<Retouchorder>();

    public virtual User User { get; set; } = null!;
}
