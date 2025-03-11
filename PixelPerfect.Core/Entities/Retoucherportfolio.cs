using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Retoucherportfolio
{
    public int PortfolioId { get; set; }

    public int RetoucherId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public bool? IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Portfolioitem> Portfolioitems { get; set; } = new List<Portfolioitem>();

    public virtual Retoucher Retoucher { get; set; } = null!;
}
