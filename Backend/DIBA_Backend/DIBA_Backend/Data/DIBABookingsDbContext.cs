using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Data
{
    public class DIBABookingsDbContext : DbContext
    {
        public DIBABookingsDbContext(DbContextOptions<DIBABookingsDbContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingStatus> BookingStatuses { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<VenueFeature> VenueFeatures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // ROLE → USER
            // One Role can have many Users
            // =========================================================

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → AUDIT LOG
            // One User can have many Audit Logs
            // =========================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → BOOKING
            // One User can create many Bookings
            // =========================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // EVENT → BOOKING
            // One Event can have many Bookings
            // =========================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → BOOKING
            // One Venue can have many Bookings
            // =========================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // BOOKING STATUS → BOOKING
            // One Status can have many Bookings
            // =========================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BookingStatus)
                .WithMany(bs => bs.Bookings)
                .HasForeignKey(b => b.BookingStatusId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → EVENT
            // One Venue can host many Events
            // =========================================================

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → EVENT
            // One User can create many Events
            // =========================================================

            modelBuilder.Entity<Event>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → VENUE FEATURE
            // One Venue can have many Features
            // =========================================================

            modelBuilder.Entity<VenueFeature>()
                .HasOne(vf => vf.Venue)
                .WithMany(v => v.VenueFeatures)
                .HasForeignKey(vf => vf.VenueId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // BOOKING → PAYMENT
            // One Booking can have many Payments
            // =========================================================

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);


            // =========================================================
            // USER → NOTIFICATION
            // One User can have many Notifications
            // =========================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // BOOKING → NOTIFICATION
            // One Booking can have many Notifications
            // =========================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Booking)
                .WithMany(b => b.Notifications)
                .HasForeignKey(n => n.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Venue>()
                .Property(v => v.Price)
                .HasPrecision(18, 2);


            // =========================================================
            // SEED ROLES
            // =========================================================

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    RoleId = new Guid("11111111-1111-1111-1111-111111111111"),
                    RoleName = "Administrator"
                },
                new Role
                {
                    RoleId = new Guid("22222222-2222-2222-2222-222222222222"),
                    RoleName = "Staff"
                },
                new Role
                {
                    RoleId = new Guid("33333333-3333-3333-3333-333333333333"),
                    RoleName = "Event Organiser"
                }
            );


            // =========================================================
            // SEED BOOKING STATUSES
            // =========================================================

            modelBuilder.Entity<BookingStatus>().HasData(
                new BookingStatus
                {
                    BookingStatusId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    StatusName = "Pending",
                    StatusDescription = "Booking request is awaiting approval."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    StatusName = "Approved",
                    StatusDescription = "Booking has been approved."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    StatusName = "Rejected",
                    StatusDescription = "Booking request has been rejected."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    StatusName = "Cancelled",
                    StatusDescription = "Booking has been cancelled."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    StatusName = "Completed",
                    StatusDescription = "Booking has been completed."
                }
            );
        }
    }
}