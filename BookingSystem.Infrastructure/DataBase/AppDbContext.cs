// Backend purpose: Maps domain entities and database constraints through Entity Framework Core.

using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Document = BookingSystem.Domain.Entities.Document;

namespace BookingSystem.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<StaffingStatus> StaffingStatuses => Set<StaffingStatus>();
        public DbSet<StandbyEntry> StandbyEntries => Set<StandbyEntry>();

     
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // The Patients table is managed outside the current migration chain, so migrations must not recreate it.
            modelBuilder.Entity<Patient>().ToTable("Patients", t => t.ExcludeFromMigrations());

            // Hide soft-deleted users from normal queries without requiring filters in every handler.
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

            // SQL Server enforces one active booking per minute, which closes concurrent double-booking races.
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.Date)
                .IsUnique()
                .HasFilter($"[{nameof(Booking.Status)}] <> {(int)BookingStatus.Cancelled}");

            // A date can have only one staffing summary, updated through the staffing endpoint.
            modelBuilder.Entity<StaffingStatus>()
                .HasIndex(s => s.Date)
                .IsUnique();

            // A patient may have only one active standby entry while historical inactive rows remain available.
            modelBuilder.Entity<StandbyEntry>()
                .HasIndex(s => new { s.PatientId, s.IsActive })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            base.OnModelCreating(modelBuilder);
        }        
    }
}
