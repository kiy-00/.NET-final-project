using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class Photographer
{
    public int PhotographerId { get; set; }

    public int UserId { get; set; }

    public string? Bio { get; set; }

    public string? Experience { get; set; }

    public string? EquipmentInfo { get; set; }

    public string? Location { get; set; }

    public decimal? PriceRangeMin { get; set; }

    public decimal? PriceRangeMax { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Photographerportfolio> Photographerportfolios { get; set; } = new List<Photographerportfolio>();

    public virtual User User { get; set; } = null!;
}
