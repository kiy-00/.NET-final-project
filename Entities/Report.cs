using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class Report
{
    public int ReportId { get; set; }

    public int UserId { get; set; }

    public int PostId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? HandledAt { get; set; }

    public int? HandledByUserId { get; set; }

    public virtual User? HandledByUser { get; set; }

    public virtual Post Post { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
