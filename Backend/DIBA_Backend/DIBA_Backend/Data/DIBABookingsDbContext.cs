using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Data
{
    // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
    // EF Core describes a one-to-many relationship as a situation where
    // "a single entity is associated with any number of other entities."
    //
    // DIBA adaptation:
    // DIBABookingsDbContext is the main EF Core database context for the
    // application. It represents the database and provides access to the
    // application's tables through DbSet properties.
    //
    // The OnModelCreating method below is used to explicitly tell EF Core
    // how the DIBA entities are related to each other, which foreign keys
    // they use, and what should happen when related records are deleted.
    public class DIBABookingsDbContext : DbContext
    {
        public DIBABookingsDbContext(
            DbContextOptions<DIBABookingsDbContext> options)
            : base(options)
        {
        }


        // =========================================================
        // TABLES
        // =========================================================

        // Each DbSet represents an entity collection that EF Core maps
        // to a database table.
        //
        // For example:
        // _dbContext.Users allows the application to query the Users table.
        // _dbContext.Bookings allows the application to query the Bookings table.
        //
        // These DbSets are used throughout the controllers to create,
        // retrieve, update and delete database records.

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

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            // Microsoft demonstrates that a one-to-many relationship can be
            // configured using HasMany(), WithOne() and HasForeignKey().
            //
            // How this works:
            //
            // HasMany(r => r.Users)
            //     means that one Role can be connected to many User records.
            //
            // WithOne(u => u.Role)
            //     means that each User has one related Role.
            //
            // HasForeignKey(u => u.RoleId)
            //     tells EF Core that User.RoleId is the foreign key that
            //     connects the User to the Role table.
            //
            // In database terms:
            //
            // Roles.RoleId
            //       ↑
            //       |
            // Users.RoleId
            //
            // DIBA example:
            // Administrator → many administrator users
            // Staff         → many staff users
            // Event Organiser → many event organisers
            //
            // Therefore, Role is the principal/parent entity and User is
            // the dependent/child entity.
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)

                // Reference: Microsoft Learn, "Cascade Delete - EF Core".
                // OnDelete() controls what happens to related records when
                // the principal entity is deleted.
                //
                // Restrict means EF Core/database should not automatically
                // delete the related Users when a Role is deleted.
                //
                // DIBA reason:
                // A role is important to the identity and authorization
                // system. Automatically deleting users because a role was
                // removed would be dangerous.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → AUDIT LOG
            // One User can have many Audit Logs
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // HasOne(a => a.User)
            //     means an AuditLog can reference one User.
            //
            // WithMany()
            //     means a User can be associated with many AuditLog records.
            //
            // HasForeignKey(a => a.UserId)
            //     identifies AuditLog.UserId as the foreign key.
            //
            // DIBA example:
            // If an Administrator approves a booking, an AuditLog record
            // can store the Administrator's UserId so that the system knows
            // who performed the action.
            //
            // The relationship therefore allows the system to answer:
            // "Which user performed this administrative action?"
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)

                // DIBA reason:
                // Audit history should not disappear simply because a user
                // record is removed. Restrict protects historical audit data
                // from automatic cascading deletion.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → BOOKING
            // One User can create many Bookings
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // A User is the principal entity and Booking is the dependent
            // entity.
            //
            // HasOne(b => b.User)
            //     A booking belongs to one user.
            //
            // WithMany()
            //     A user can have many bookings.
            //
            // HasForeignKey(b => b.UserId)
            //     Booking.UserId identifies the user who owns the booking.
            //
            // This relationship is also important for access control.
            // For example, the application can check Booking.UserId against
            // the authenticated user's ID before allowing the user to view
            // or modify their booking.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)

                // DIBA reason:
                // Existing bookings are business records and should not
                // automatically disappear because a User is deleted.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // EVENT → BOOKING
            // One Event can have many Bookings
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // HasOne(b => b.Event)
            //     Each booking is associated with one Event.
            //
            // WithMany(e => e.Bookings)
            //     One Event can contain many Booking records.
            //
            // HasForeignKey(b => b.EventId)
            //     Booking.EventId is the foreign key connecting the booking
            //     to its event.
            //
            // DIBA example:
            // An Event Organiser can create an event and a booking for it.
            // The Event then becomes the parent of the associated booking.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)

                // DIBA reason:
                // Events are part of the booking history, so deleting an
                // Event should not automatically delete its booking records.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → BOOKING
            // One Venue can have many Bookings
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // HasOne(b => b.Venue)
            //     Each Booking uses one Venue.
            //
            // WithMany(v => v.Bookings)
            //     One Venue can be associated with many bookings over time.
            //
            // HasForeignKey(b => b.VenueId)
            //     Booking.VenueId identifies the venue being booked.
            //
            // This relationship is important to the DIBA booking system
            // because the application uses the venue ID when checking
            // whether a venue is already booked for a requested period.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)

                // DIBA reason:
                // Venue records should remain available as part of the
                // historical booking information.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // BOOKING STATUS → BOOKING
            // One Status can have many Bookings
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // A BookingStatus is the principal entity.
            //
            // HasOne(b => b.BookingStatus)
            //     Each booking has one current status.
            //
            // WithMany(bs => bs.Bookings)
            //     The same status can be assigned to many bookings.
            //
            // HasForeignKey(b => b.BookingStatusId)
            //     BookingStatusId connects the booking to its status.
            //
            // DIBA example:
            //
            // Pending
            //     ↓
            // Approved / Rejected
            //
            // A single "Pending" status record can therefore be referenced
            // by many different bookings.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BookingStatus)
                .WithMany(bs => bs.Bookings)
                .HasForeignKey(b => b.BookingStatusId)

                // DIBA reason:
                // The status definitions are shared by many bookings, so
                // deleting a status should not automatically delete bookings.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → EVENT
            // One Venue can host many Events
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // Each Event is associated with one Venue, while a Venue can
            // host multiple Events over time.
            //
            // VenueId is stored on Event as the foreign key.
            //
            // DIBA example:
            // A venue such as a conference hall can host different events
            // on different dates.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)

                // DIBA reason:
                // Historical events should not disappear automatically when
                // a venue record is removed.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER → EVENT
            // One User can create many Events
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // Each Event has one creating User, while one User can create
            // multiple Events.
            //
            // Event.UserId is the foreign key connecting the Event to the
            // User who created it.
            //
            // DIBA use:
            // This relationship allows the system to associate an event
            // with the Event Organiser who created it.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)

                // DIBA reason:
                // Event ownership/history should remain available rather than
                // being removed automatically with a user deletion.
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // VENUE → VENUE FEATURE
            // One Venue can have many Features
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // HasOne(vf => vf.Venue)
            //     Each VenueFeature belongs to one Venue.
            //
            // WithMany(v => v.VenueFeatures)
            //     One Venue can have many features.
            //
            // HasForeignKey(vf => vf.VenueId)
            //     VenueFeature.VenueId identifies the parent Venue.
            //
            // DIBA example:
            //
            // Venue
            //   ├── Projector
            //   ├── Wi-Fi
            //   ├── Parking
            //   └── Sound System
            //
            // This creates a parent-child relationship between a venue
            // and the features offered by that venue.
            modelBuilder.Entity<VenueFeature>()
                .HasOne(vf => vf.Venue)
                .WithMany(v => v.VenueFeatures)
                .HasForeignKey(vf => vf.VenueId)

                // Reference: Microsoft Learn, "Cascade Delete - EF Core".
                // Microsoft explains that cascade delete can automatically
                // delete dependent records when the principal is deleted.
                //
                // DIBA reason:
                // Venue features belong directly to their venue and have
                // no useful independent meaning without that venue.
                //
                // Therefore, if a venue is deleted, its associated
                // VenueFeature records can also be deleted.
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // BOOKING → PAYMENT
            // One Booking can have many Payments
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // Each Payment belongs to one Booking.
            //
            // WithMany(b => b.Payments) means that a Booking can have
            // multiple Payment records.
            //
            // BookingId is the foreign key connecting the Payment to
            // its Booking.
            //
            // DIBA use:
            // Payments are linked to the booking for which the payment
            // information was recorded.
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)

                // DIBA reason:
                // Payment records are dependent on the booking.
                // If the booking is permanently removed, its dependent
                // payment records can also be removed.
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // PAYMENT AMOUNT PRECISION
            // =========================================================

            // DIBA-specific database configuration:
            //
            // Amount is a financial value, so the database should store
            // two decimal places rather than relying on a default numeric
            // precision.
            //
            // HasPrecision(18, 2) means:
            //
            // 18 = maximum number of digits in the value.
            // 2  = number of digits after the decimal point.
            //
            // Example:
            // 12500.50
            //
            // This is important for payment amounts because monetary values
            // need predictable decimal storage.
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);


            // =========================================================
            // USER → NOTIFICATION
            // One User can have many Notifications
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // Each Notification belongs to one User, while one User can
            // receive many notifications.
            //
            // Notification.UserId is the foreign key.
            //
            // DIBA example:
            // When a booking is approved, rejected or cancelled, the system
            // can create a notification associated with the relevant user.
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)

                // DIBA reason:
                // Notifications belong to the user's account and can be
                // removed together with the user.
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // BOOKING → NOTIFICATION
            // One Booking can have many Notifications
            // =========================================================

            // Reference: Microsoft Learn, "One-to-many relationships - EF Core".
            //
            // A Notification can be associated with one Booking.
            //
            // WithMany(b => b.Notifications) means one Booking can have
            // multiple notifications.
            //
            // DIBA example:
            //
            // Booking
            //    ↓
            //    ├── "Booking approved"
            //    ├── "Payment required"
            //    └── "Booking cancelled"
            //
            // The notifications therefore remain connected to the booking
            // that caused the notification.
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Booking)
                .WithMany(b => b.Notifications)
                .HasForeignKey(n => n.BookingId)

                // DIBA reason:
                // Notifications associated only with a particular booking
                // do not need to remain when that booking is deleted.
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // SEED ROLES
            // =========================================================

            // Reference: Microsoft Learn, "Data Seeding - EF Core".
            //
            // EF Core's HasData() allows predefined model-managed data to
            // be included in the database configuration.
            //
            // Microsoft explains that HasData values are converted into
            // InsertData, UpdateData and DeleteData operations when a
            // migration is created.
            //
            // DIBA use:
            // The three roles are required by the application, so they are
            // predefined rather than requiring an Administrator to create
            // them manually before the system can be used.
            //
            // Fixed GUIDs are deliberately used because the seeded role
            // identifiers need to remain stable between migrations.
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

            // Reference: Microsoft Learn, "Data Seeding - EF Core".
            //
            // These are also model-managed records because the booking
            // workflow depends on a fixed set of statuses.
            //
            // DIBA booking workflow:
            //
            // Pending
            //     ↓
            // Approved
            //     OR
            // Rejected
            //
            // A booking can later become:
            // Cancelled
            // Completed
            //
            // Keeping these statuses as predefined records means that
            // controllers can reference the status IDs and status names
            // consistently instead of allowing users to create arbitrary
            // booking states.
            modelBuilder.Entity<BookingStatus>().HasData(
                new BookingStatus
                {
                    BookingStatusId = new Guid(
                        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    StatusName = "Pending",
                    StatusDescription =
                        "Booking request is awaiting approval."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid(
                        "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    StatusName = "Approved",
                    StatusDescription =
                        "Booking has been approved."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid(
                        "cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    StatusName = "Rejected",
                    StatusDescription =
                        "Booking request has been rejected."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid(
                        "dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    StatusName = "Cancelled",
                    StatusDescription =
                        "Booking has been cancelled."
                },
                new BookingStatus
                {
                    BookingStatusId = new Guid(
                        "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    StatusName = "Completed",
                    StatusDescription =
                        "Booking has been completed."
                }
            );
        }
    }
}