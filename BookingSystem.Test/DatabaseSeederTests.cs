using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

[TestFixture]
public class DatabaseSeederTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Test]
    public void SeedData_AddsPatients_WhenPatientsAreEmpty()
    {
        using var context = CreateInMemoryContext();
        var seeder = new DatabaseSeeder(context);

        seeder.SeedData();

        Assert.That(context.Patients.Any(), Is.True);
    }

    [Test]
    public void SeedData_AddsBookings_WhenBookingsAreEmpty()
    {
        using var context = CreateInMemoryContext();
        var seeder = new DatabaseSeeder(context);

        seeder.SeedData();

        Assert.That(context.Bookings.Any(), Is.True);
    }
}
