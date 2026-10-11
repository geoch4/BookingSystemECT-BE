using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BookingSystem.Test;

[NonParallelizable]
public class RbacMigrationSqlTests
{
    private sealed class TestSchema(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Patient>().ToTable("Patients", t => t.ExcludeFromMigrations(false));
        }
    }

    [Test, Explicit("Creates and removes a uniquely named isolated SQL database")]
    public async Task Three_Rbac_Migrations_Upgrade_Legacy_Data_Without_Inferring_Treatment()
    {
        var database = "RbacTest_" + Guid.NewGuid().ToString("N");
        var server = Environment.GetEnvironmentVariable("RBAC_SQL_SERVER") ?? @".\SQLEXPRESS";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer($"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10").Options;
        await using var db = new AppDbContext(options);
        try
        {
            await using (var schema = new TestSchema(options)) await schema.Database.EnsureCreatedAsync();
            // Reconstruct the immediately preceding schema; the patient table is externally managed.
            await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);");
            foreach (var migration in db.Database.GetMigrations())
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [__EFMigrationsHistory] VALUES ({migration}, {"8.0.11"})");
            var user = new User { Fullname="Synthetic", Email="upgrade@rbac.test", PasswordHash="synthetic", Role=new Role {RoleName="Avdelning"}, CareUnitId="D1" };
            var patient = new Patient { FullName="Synthetic upgrade", SocialSecurityNumber="UPGRADE-TEST" };
            db.Users.Add(user); db.Patients.Add(patient); await db.SaveChangesAsync();
            db.Bookings.Add(new Booking {PatientId=patient.PatientId, CreatedByUserId=user.UserId, Date=new DateTime(2026,10,11,9,0,0), CareUnitId="D1"});
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261006120000_AddDepartmentPriorityToBookings");
            // Use a matching duplicate audit source for the same department.
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO AuditLogs (EntityName,EntityId,Action,CreatedAt,Role,CareUnitId)
                SELECT N'Patient',CONVERT(nvarchar(80),PatientId),N'Added',SYSUTCDATETIME(),N'Avdelning',N'D1' FROM Patients;
            ");
            await migrator.MigrateAsync();
            var booking = await db.Bookings.SingleAsync();
            Assert.That(booking.DepartmentId, Is.EqualTo("D1"));
            Assert.That(booking.TreatmentUnitId, Is.Null);
            var relation = await db.PatientCareUnits.SingleAsync();
            Assert.That(relation.UnitId, Is.EqualTo("D1"));
            Assert.That(relation.Kind, Is.EqualTo("department"));
            Assert.That(relation.IsActive, Is.True);
            var version = (await db.Users.SingleAsync()).SessionVersion;
            Assert.That(version.Length, Is.EqualTo(32));
            Assert.That(Guid.TryParseExact(version,"N",out _), Is.True);
            await migrator.MigrateAsync();
            Assert.That(await db.PatientCareUnits.CountAsync(), Is.EqualTo(1));
            Assert.That((await db.Users.AsNoTracking().SingleAsync()).SessionVersion, Is.EqualTo(version));
            Assert.That(await db.Database.GetPendingMigrationsAsync(), Is.Empty);
        }
        finally
        {
            if (!database.StartsWith("RbacTest_",StringComparison.Ordinal) || database.Length != 41) throw new InvalidOperationException("Unsafe cleanup target");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
