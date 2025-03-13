using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Portfolioitem
{
    public int ItemId { get; set; }

    public int PortfolioId { get; set; }

    public string PortfolioType { get; set; } = null!;

    public string ImagePath { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool? IsBeforeImage { get; set; }

    public int? AfterImageId { get; set; }

    public virtual Portfolioitem? AfterImage { get; set; }

    public virtual ICollection<Portfolioitem> InverseAfterImage { get; set; } = new List<Portfolioitem>();

    public virtual Photographerportfolio Portfolio { get; set; } = null!;

    public virtual Retoucherportfolio PortfolioNavigation { get; set; } = null!;
}
