using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int PhotographerId { get; set; }

    public DateTime BookingDate { get; set; }

    public string Location { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal InitialAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public string? Requirements { get; set; }

    public int PhotoCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsPublic { get; set; }

    public virtual ICollection<Bookingservice> Bookingservices { get; set; } = new List<Bookingservice>();

    public virtual Photographer Photographer { get; set; } = null!;

    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    public virtual User User { get; set; } = null!;
}
