using System;
using System.Collections.Generic;

namespace PixelPerfect.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// 用户个人简介
    /// </summary>
    public string? Biography { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Follow> FollowFolloweds { get; set; } = new List<Follow>();

    public virtual ICollection<Follow> FollowFollowers { get; set; } = new List<Follow>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Photographer? Photographer { get; set; }

    public virtual ICollection<Post> PostApprovedByUsers { get; set; } = new List<Post>();

    public virtual ICollection<Post> PostUsers { get; set; } = new List<Post>();

    public virtual ICollection<Report> ReportHandledByUsers { get; set; } = new List<Report>();

    public virtual ICollection<Report> ReportUsers { get; set; } = new List<Report>();

    public virtual Retoucher? Retoucher { get; set; }

    public virtual ICollection<Retouchorder> Retouchorders { get; set; } = new List<Retouchorder>();

    public virtual ICollection<Userrole> Userroles { get; set; } = new List<Userrole>();
}
