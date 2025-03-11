using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace PixelPerfect.Core.Entities;

public partial class PhotoBookingDbContext : DbContext
{
    public PhotoBookingDbContext()
    {
    }

    public PhotoBookingDbContext(DbContextOptions<PhotoBookingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Bookingservice> Bookingservices { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Photo> Photos { get; set; }

    public virtual DbSet<Photographer> Photographers { get; set; }

    public virtual DbSet<Photographerportfolio> Photographerportfolios { get; set; }

    public virtual DbSet<Portfolioitem> Portfolioitems { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Retoucher> Retouchers { get; set; }

    public virtual DbSet<Retoucherportfolio> Retoucherportfolios { get; set; }

    public virtual DbSet<Retouchorder> Retouchorders { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFollowersView> UserFollowersViews { get; set; }

    public virtual DbSet<UserFollowingView> UserFollowingViews { get; set; }

    public virtual DbSet<Userrole> Userroles { get; set; }

    public virtual DbSet<Userrolesview> Userrolesviews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=PhotoBookingDB;uid=root;pwd=IamSherlocked623", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.4.4-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PRIMARY");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.PhotographerId, "fk_photographer_booking");

            entity.HasIndex(e => e.UserId, "fk_user_booking");

            entity.HasIndex(e => e.BookingDate, "idx_bookingdate");

            entity.HasIndex(e => e.Location, "idx_location");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.FinalAmount).HasPrecision(10, 2);
            entity.Property(e => e.InitialAmount).HasPrecision(10, 2);
            entity.Property(e => e.PhotographerId).HasColumnName("PhotographerID");
            entity.Property(e => e.Requirements).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Confirmed','InProgress','Completed','Cancelled')");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Photographer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PhotographerId)
                .HasConstraintName("fk_photographer_booking");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_booking");
        });

        modelBuilder.Entity<Bookingservice>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PRIMARY");

            entity.ToTable("bookingservices");

            entity.HasIndex(e => e.BookingId, "idx_booking");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.ServiceName).HasMaxLength(100);

            entity.HasOne(d => d.Booking).WithMany(p => p.Bookingservices)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("fk_booking_service");
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PRIMARY");

            entity.ToTable("follows");

            entity.HasIndex(e => e.FollowedId, "idx_followed");

            entity.HasIndex(e => e.FollowerId, "idx_follower");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.HasIndex(e => new { e.FollowerId, e.FollowedId }, "uk_follower_followed").IsUnique();

            entity.Property(e => e.FollowId).HasColumnName("FollowID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.FollowedId)
                .HasComment("被关注者ID")
                .HasColumnName("FollowedID");
            entity.Property(e => e.FollowerId)
                .HasComment("关注者ID")
                .HasColumnName("FollowerID");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Blocked')");

            entity.HasOne(d => d.Followed).WithMany(p => p.FollowFolloweds)
                .HasForeignKey(d => d.FollowedId)
                .HasConstraintName("fk_followed_user");

            entity.HasOne(d => d.Follower).WithMany(p => p.FollowFollowers)
                .HasForeignKey(d => d.FollowerId)
                .HasConstraintName("fk_follower_user");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.LikeId).HasName("PRIMARY");

            entity.ToTable("likes");

            entity.HasIndex(e => e.PostId, "idx_post");

            entity.HasIndex(e => new { e.UserId, e.PostId }, "uk_user_post_like").IsUnique();

            entity.Property(e => e.LikeId).HasColumnName("LikeID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Post).WithMany(p => p.Likes)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("fk_post_like");

            entity.HasOne(d => d.User).WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_like");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.IsRead, "idx_isread");

            entity.HasIndex(e => e.Type, "idx_type");

            entity.HasIndex(e => e.UserId, "idx_user");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Type).HasColumnType("enum('System','Booking','Interaction','Payment','Follow')");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_notification");
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PRIMARY");

            entity.ToTable("photos");

            entity.HasIndex(e => e.BookingId, "idx_booking");

            entity.HasIndex(e => e.IsPublic, "idx_ispublic");

            entity.Property(e => e.PhotoId).HasColumnName("PhotoID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ImagePath).HasMaxLength(255);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Booking).WithMany(p => p.Photos)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("fk_booking_photo");
        });

        modelBuilder.Entity<Photographer>(entity =>
        {
            entity.HasKey(e => e.PhotographerId).HasName("PRIMARY");

            entity.ToTable("photographers");

            entity.HasIndex(e => e.UserId, "UserID").IsUnique();

            entity.HasIndex(e => e.Location, "idx_location");

            entity.HasIndex(e => e.IsVerified, "idx_verified");

            entity.Property(e => e.PhotographerId).HasColumnName("PhotographerID");
            entity.Property(e => e.Bio).HasColumnType("text");
            entity.Property(e => e.EquipmentInfo).HasColumnType("text");
            entity.Property(e => e.Experience).HasColumnType("text");
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.PriceRangeMax).HasPrecision(10, 2);
            entity.Property(e => e.PriceRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithOne(p => p.Photographer)
                .HasForeignKey<Photographer>(d => d.UserId)
                .HasConstraintName("fk_user_photographer");
        });

        modelBuilder.Entity<Photographerportfolio>(entity =>
        {
            entity.HasKey(e => e.PortfolioId).HasName("PRIMARY");

            entity.ToTable("photographerportfolios");

            entity.HasIndex(e => e.PhotographerId, "fk_photographer_portfolio");

            entity.HasIndex(e => e.Category, "idx_category");

            entity.HasIndex(e => e.IsPublic, "idx_ispublic");

            entity.Property(e => e.PortfolioId).HasColumnName("PortfolioID");
            entity.Property(e => e.Category).HasColumnType("enum('Portrait','Wedding','Fashion','Product','Event','Landscape','Other')");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.IsPublic)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.PhotographerId).HasColumnName("PhotographerID");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Photographer).WithMany(p => p.Photographerportfolios)
                .HasForeignKey(d => d.PhotographerId)
                .HasConstraintName("fk_photographer_portfolio");
        });

        modelBuilder.Entity<Portfolioitem>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PRIMARY");

            entity.ToTable("portfolioitems");

            entity.HasIndex(e => e.AfterImageId, "fk_after_image");

            entity.HasIndex(e => e.PortfolioId, "idx_portfolio");

            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.AfterImageId).HasColumnName("AfterImageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ImagePath).HasMaxLength(255);
            entity.Property(e => e.IsBeforeImage).HasDefaultValueSql("'0'");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.PortfolioId).HasColumnName("PortfolioID");
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.AfterImage).WithMany(p => p.InverseAfterImage)
                .HasForeignKey(d => d.AfterImageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_after_image");

            entity.HasOne(d => d.Portfolio).WithMany(p => p.Portfolioitems)
                .HasForeignKey(d => d.PortfolioId)
                .HasConstraintName("fk_portfolio_item");

            entity.HasOne(d => d.PortfolioNavigation).WithMany(p => p.Portfolioitems)
                .HasForeignKey(d => d.PortfolioId)
                .HasConstraintName("fk_retoucher_portfolio_item");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PRIMARY");

            entity.ToTable("posts");

            entity.HasIndex(e => e.ApprovedByUserId, "fk_admin_approval");

            entity.HasIndex(e => e.UserId, "fk_user_post");

            entity.HasIndex(e => new { e.Title, e.Content }, "ftx_post_content").HasAnnotation("MySql:FullTextIndex", true);

            entity.HasIndex(e => e.CreatedAt, "idx_createdat");

            entity.HasIndex(e => e.IsApproved, "idx_isapproved");

            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.ApprovedAt).HasColumnType("datetime");
            entity.Property(e => e.ApprovedByUserId).HasColumnName("ApprovedByUserID");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagePath).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.PostApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_admin_approval");

            entity.HasOne(d => d.User).WithMany(p => p.PostUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_post");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.HasIndex(e => e.HandledByUserId, "fk_admin_handling");

            entity.HasIndex(e => e.UserId, "fk_user_report");

            entity.HasIndex(e => e.PostId, "idx_post");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.HandledAt).HasColumnType("datetime");
            entity.Property(e => e.HandledByUserId).HasColumnName("HandledByUserID");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.Reason).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Reviewed','Actioned','Dismissed')");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.HandledByUser).WithMany(p => p.ReportHandledByUsers)
                .HasForeignKey(d => d.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_admin_handling");

            entity.HasOne(d => d.Post).WithMany(p => p.Reports)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("fk_post_report");

            entity.HasOne(d => d.User).WithMany(p => p.ReportUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_report");
        });

        modelBuilder.Entity<Retoucher>(entity =>
        {
            entity.HasKey(e => e.RetoucherId).HasName("PRIMARY");

            entity.ToTable("retouchers");

            entity.HasIndex(e => e.UserId, "UserID").IsUnique();

            entity.HasIndex(e => e.IsVerified, "idx_verified");

            entity.Property(e => e.RetoucherId).HasColumnName("RetoucherID");
            entity.Property(e => e.Bio).HasColumnType("text");
            entity.Property(e => e.Expertise).HasColumnType("text");
            entity.Property(e => e.PricePerPhoto).HasPrecision(10, 2);
            entity.Property(e => e.Software).HasColumnType("text");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithOne(p => p.Retoucher)
                .HasForeignKey<Retoucher>(d => d.UserId)
                .HasConstraintName("fk_user_retoucher");
        });

        modelBuilder.Entity<Retoucherportfolio>(entity =>
        {
            entity.HasKey(e => e.PortfolioId).HasName("PRIMARY");

            entity.ToTable("retoucherportfolios");

            entity.HasIndex(e => e.RetoucherId, "fk_retoucher_portfolio");

            entity.HasIndex(e => e.Category, "idx_category");

            entity.HasIndex(e => e.IsPublic, "idx_ispublic");

            entity.Property(e => e.PortfolioId).HasColumnName("PortfolioID");
            entity.Property(e => e.Category).HasColumnType("enum('Portrait','Wedding','Fashion','Product','Event','Other','Landscape')");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.IsPublic)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.RetoucherId).HasColumnName("RetoucherID");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Retoucher).WithMany(p => p.Retoucherportfolios)
                .HasForeignKey(d => d.RetoucherId)
                .HasConstraintName("fk_retoucher_portfolio");
        });

        modelBuilder.Entity<Retouchorder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("retouchorders");

            entity.HasIndex(e => e.UserId, "fk_user_retouch");

            entity.HasIndex(e => e.PhotoId, "idx_photo");

            entity.HasIndex(e => e.RetoucherId, "idx_retoucher");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.PhotoId).HasColumnName("PhotoID");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Requirements).HasColumnType("text");
            entity.Property(e => e.RetoucherId).HasColumnName("RetoucherID");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','InProgress','Completed','Cancelled')");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Photo).WithMany(p => p.Retouchorders)
                .HasForeignKey(d => d.PhotoId)
                .HasConstraintName("fk_photo_retouch");

            entity.HasOne(d => d.Retoucher).WithMany(p => p.Retouchorders)
                .HasForeignKey(d => d.RetoucherId)
                .HasConstraintName("fk_retoucher_order");

            entity.HasOne(d => d.User).WithMany(p => p.Retouchorders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_retouch");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.Username, "idx_username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Biography)
                .HasComment("用户个人简介")
                .HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Salt).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<UserFollowersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("user_followers_view");

            entity.Property(e => e.Followers).HasColumnType("text");
            entity.Property(e => e.UserId)
                .HasComment("被关注者ID")
                .HasColumnName("UserID");
        });

        modelBuilder.Entity<UserFollowingView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("user_following_view");

            entity.Property(e => e.Following).HasColumnType("text");
            entity.Property(e => e.UserId)
                .HasComment("关注者ID")
                .HasColumnName("UserID");
        });

        modelBuilder.Entity<Userrole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PRIMARY");

            entity.ToTable("userroles");

            entity.HasIndex(e => e.RoleType, "idx_roletype");

            entity.HasIndex(e => new { e.UserId, e.RoleType }, "uk_user_role").IsUnique();

            entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.RoleType).HasColumnType("enum('Regular','Photographer','Retoucher','Admin')");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Userroles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<Userrolesview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("userrolesview");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Roles).HasColumnType("text");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
