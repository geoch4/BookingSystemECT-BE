// Backend purpose: Maps domain entities and database constraints through Entity Framework Core.

using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Document = BookingSystem.Domain.Entities.Document;

namespace BookingSystem.Infrastructure
{
    public class AppDbContext : DbContext
    {
        private readonly IAuditContext? _auditContext;
        private bool _isSavingAudit;

        public AppDbContext(DbContextOptions<AppDbContext> options, IAuditContext? auditContext = null) : base(options)
        {
            _auditContext = auditContext;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<PatientCareUnit> PatientCareUnits => Set<PatientCareUnit>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ReferralAction> ReferralActions => Set<ReferralAction>();
        public DbSet<ReferralClinicalDetails> ReferralClinicalDetails => Set<ReferralClinicalDetails>();
        public DbSet<StaffingStatus> StaffingStatuses => Set<StaffingStatus>();
        public DbSet<StandbyEntry> StandbyEntries => Set<StandbyEntry>();

     
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // The Patients table is managed outside the current migration chain, so migrations must not recreate it.
            modelBuilder.Entity<Patient>().ToTable("Patients", t => t.ExcludeFromMigrations());

            modelBuilder.Entity<PatientCareUnit>().HasKey(m => new {m.PatientId, m.UnitId, m.Kind});
            modelBuilder.Entity<PatientCareUnit>().HasOne(m => m.Patient).WithMany().HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PatientCareUnit>().HasIndex(m => new {m.UnitId, m.Kind, m.IsActive});
            // Hide soft-deleted users from normal queries without requiring filters in every handler.
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

            // A confirmed booking owns its minute at its treatment unit. Pending referrals never
            // occupy a slot, and other units keep their own schedules. SQL Server treats NULL as one
            // value in a unique index, so confirmed bookings without a unit still guard each other.
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.TreatmentUnitId, b.Date })
                .IsUnique()
                .HasFilter($"[{nameof(Booking.Status)}] = {(int)BookingStatus.Confirmed}");

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.ClinicalDetails)
                .WithOne(d => d.Booking)
                .HasForeignKey<ReferralClinicalDetails>(d => d.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Historical records should remain visible even if the associated user is later soft-deleted.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey(b => b.CreatedByUserId)
                .IsRequired(false);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .IsRequired(false);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .IsRequired(false);

            // A date can have only one staffing summary, updated through the staffing endpoint.
            modelBuilder.Entity<StaffingStatus>()
                .HasIndex(s => s.Date)
                .IsUnique();

            // A patient may have only one active standby entry while historical inactive rows remain available.
            modelBuilder.Entity<StandbyEntry>()
                .HasIndex(s => new { s.PatientId, s.IsActive })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<ReferralAction>()
                .HasIndex(a => a.ReferralId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityName, a.EntityId, a.CreatedAt });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            if (_isSavingAudit) return base.SaveChanges();

            RotateChangedSessions();
            var pendingAudits = BuildAuditLogs();
            var result = base.SaveChanges();
            SaveAuditLogs(pendingAudits);
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_isSavingAudit) return await base.SaveChangesAsync(cancellationToken);

            RotateChangedSessions();
            var pendingAudits = BuildAuditLogs();
            var result = await base.SaveChangesAsync(cancellationToken);
            await SaveAuditLogsAsync(pendingAudits, cancellationToken);
            return result;
        }

        private void RotateChangedSessions()
        {
            ChangeTracker.DetectChanges();
            var changedRoles=ChangeTracker.Entries<Role>().Where(e=>e.State==EntityState.Modified && e.Property(nameof(Role.RoleName)).IsModified).Select(e=>e.Entity.RoleId).ToArray();
            if (changedRoles.Length>0)
                foreach (var user in Users.Where(u=>changedRoles.Contains(u.RoleId)).ToList()) user.SessionVersion=Guid.NewGuid().ToString("N");
            foreach (var entry in ChangeTracker.Entries<User>().Where(e=>e.State==EntityState.Modified))
                if (new[] {nameof(User.RoleId),nameof(User.CareUnitId),nameof(User.PasswordHash),nameof(User.Email),nameof(User.IsDeleted)}.Any(name=>entry.Property(name).IsModified))
                    entry.Entity.SessionVersion=Guid.NewGuid().ToString("N");
        }

        private List<PendingAuditLog> BuildAuditLogs()
        {
            ChangeTracker.DetectChanges();

            return ChangeTracker.Entries()
                .Where(entry => entry.Entity is not AuditLog &&
                                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => new PendingAuditLog(
                    entry,
                    new AuditLog
                    {
                        EntityName = entry.Metadata.ClrType.Name,
                        EntityId = GetPrimaryKeyValue(entry),
                        Action = entry.State.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        UserId = _auditContext?.UserId,
                        UserName = _auditContext?.UserName ?? "system",
                        Role = _auditContext?.Role,
                        CareUnitId = _auditContext?.CareUnitId,
                        IpAddress = _auditContext?.IpAddress,
                        RequestMethod = _auditContext?.RequestMethod,
                        RequestPath = _auditContext?.RequestPath,
                        Reason = _auditContext?.Reason,
                        ChangedFields = BuildChangedFields(entry)
                    }))
                .ToList();
        }

        private void SaveAuditLogs(List<PendingAuditLog> pendingAudits)
        {
            if (pendingAudits.Count == 0) return;

            foreach (var pendingAudit in pendingAudits)
            {
                if (string.IsNullOrWhiteSpace(pendingAudit.AuditLog.EntityId) || pendingAudit.AuditLog.EntityId == "0")
                    pendingAudit.AuditLog.EntityId = GetPrimaryKeyValue(pendingAudit.Entry);

                AuditLogs.Add(pendingAudit.AuditLog);
            }

            _isSavingAudit = true;
            try
            {
                base.SaveChanges();
            }
            finally
            {
                _isSavingAudit = false;
            }
        }

        private async Task SaveAuditLogsAsync(List<PendingAuditLog> pendingAudits, CancellationToken cancellationToken)
        {
            if (pendingAudits.Count == 0) return;

            foreach (var pendingAudit in pendingAudits)
            {
                if (string.IsNullOrWhiteSpace(pendingAudit.AuditLog.EntityId) || pendingAudit.AuditLog.EntityId == "0")
                    pendingAudit.AuditLog.EntityId = GetPrimaryKeyValue(pendingAudit.Entry);

                AuditLogs.Add(pendingAudit.AuditLog);
            }

            _isSavingAudit = true;
            try
            {
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isSavingAudit = false;
            }
        }

        private static string GetPrimaryKeyValue(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey is null) return string.Empty;

            var values = primaryKey.Properties
                .Select(property => entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty);

            return string.Join(",", values);
        }

        private static string BuildChangedFields(EntityEntry entry)
        {
            var changedFields = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey()) continue;

                if (entry.State == EntityState.Modified && !property.IsModified) continue;

                var propertyName = property.Metadata.Name;
                if (IsSensitiveProperty(propertyName))
                {
                    changedFields[propertyName] = "[redacted]";
                    continue;
                }

                changedFields[propertyName] = entry.State switch
                {
                    EntityState.Added => new { New = property.CurrentValue },
                    EntityState.Deleted => new { Old = property.OriginalValue },
                    EntityState.Modified => new { Old = property.OriginalValue, New = property.CurrentValue },
                    _ => null
                };
            }

            return JsonSerializer.Serialize(changedFields);
        }

        // Clinical free text is stored once, in its own table; the audit trail records that it
        // changed and who changed it, not a second copy of the health information.
        private static readonly HashSet<string> ClinicalFreeTextProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(ReferralClinicalDetails.Indication),
            nameof(ReferralClinicalDetails.AnesthesiaInfo),
            nameof(ReferralClinicalDetails.SomaticRisk),
            nameof(ReferralClinicalDetails.BloodPressure),
            nameof(ReferralClinicalDetails.WeightKg),
            nameof(ReferralClinicalDetails.Allergies),
            nameof(ReferralClinicalDetails.Medications),
            nameof(ReferralClinicalDetails.AdditionalInfo),
            nameof(ReferralClinicalDetails.PreviousTreatments),
            nameof(ReferralClinicalDetails.TreatmentGoal)
        };

        private static bool IsSensitiveProperty(string propertyName)
        {
            return propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                   propertyName.Contains("SocialSecurityNumber", StringComparison.OrdinalIgnoreCase) ||
                   ClinicalFreeTextProperties.Contains(propertyName);
        }

        private sealed record PendingAuditLog(EntityEntry Entry, AuditLog AuditLog);
    }
}
