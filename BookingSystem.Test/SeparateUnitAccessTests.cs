using API;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BookingSystem.Test;
public class SeparateUnitAccessTests
{
    [Test]
    public void Several_Departments_Can_Send_To_One_Treatment_Unit_Without_Sharing_Department_Access()
    {
        var bookings = new[] {new Booking {DepartmentId="D1", TreatmentUnitId="T1"}, new Booking {DepartmentId="D2", TreatmentUnitId="T1"}, new Booking {DepartmentId="D2", TreatmentUnitId="T2"}}.AsQueryable();
        Assert.That(BookingAccess.Query(bookings,"Avdelning","D1").Count(), Is.EqualTo(1));
        Assert.That(BookingAccess.Query(bookings,"Behandlingsenhet","T1").Count(), Is.EqualTo(2));
        Assert.That(BookingAccess.Query(bookings,"Medicinskt ansvarig","T2").Count(), Is.EqualTo(1));
        Assert.That(BookingAccess.Query(bookings,"Admin",null).Count(), Is.EqualTo(3));
    }
    [Test]
    public void Staff_Cannot_Use_Department_Id_As_Treatment_Assignment()
    {
        Assert.That(BookingAccess.CanRead("Behandlingsenhet","D1","D1",null), Is.False);
        Assert.That(BookingAccess.CanRead("Medicinskt ansvarig","D1","D1","T1"), Is.False);
        Assert.That(BookingAccess.CanRead("Avdelning","T1","D1","T1"), Is.False);
    }
    [Test]
    public async Task Patient_Lists_Respect_Receiving_Unit()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Patients.AddRange(new Patient {PatientId=1},new Patient {PatientId=2});
        context.Bookings.AddRange(new Booking {PatientId=1, DepartmentId="D1",TreatmentUnitId="T1"}, new Booking {PatientId=2,DepartmentId="D2",TreatmentUnitId="T2"});
        context.PatientCareUnits.AddRange(new PatientCareUnit {PatientId=1,UnitId="T1",Kind="treatment"},new PatientCareUnit {PatientId=2,UnitId="T2",Kind="treatment"});
        await context.SaveChangesAsync();
        Assert.That(await CareUnitPatientAccess.Query(context,"T1","Behandlingsenhet").Select(p=>p.PatientId).ToArrayAsync(), Is.EqualTo(new[]{1}));
    }
}
