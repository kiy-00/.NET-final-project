using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class Photographerportfolio
{
    public int PortfolioId { get; set; }

    public int PhotographerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public bool? IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Photographer Photographer { get; set; } = null!;

    public virtual ICollection<Portfolioitem> Portfolioitems { get; set; } = new List<Portfolioitem>();
}
