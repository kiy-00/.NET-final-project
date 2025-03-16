using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Photo
{
    public int PhotoId { get; set; }

    public int? BookingId { get; set; }

    public string ImagePath { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Metadata { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsPublic { get; set; }

    public bool ClientApproved { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<Retouchorder> Retouchorders { get; set; } = new List<Retouchorder>();
}
