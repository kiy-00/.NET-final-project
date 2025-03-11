using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Bookingservice
{
    public int ServiceId { get; set; }

    public int BookingId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
