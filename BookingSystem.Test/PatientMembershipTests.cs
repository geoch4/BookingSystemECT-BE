using System.Security.Claims;
using API;
using API.Controllers;
using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BookingSystem.Application.Commands.PatientCommand.CreatePatient;
using BookingSystem.Application.DTO;
namespace BookingSystem.Test;
public class PatientMembershipTests
{
    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ClaimsPrincipal User(string role,string unit) => new(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,role),new Claim("care_unit",unit),new Claim(ClaimTypes.NameIdentifier,"1")},"Test"));
    [Test]
    public async Task Revocation_Is_Not_Reversed_By_Old_Booking_Or_Creation_Log()
    {
        using var context=Context();
        context.Patients.Add(new Patient {PatientId=1});
        context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId="D1",Kind="department",IsActive=false});
        context.Bookings.Add(new Booking {PatientId=1,DepartmentId="D1",TreatmentUnitId="T1"});
        context.AuditLogs.Add(new AuditLog {EntityName="Patient",EntityId="1",Action="Added",CareUnitId="D1"});
        await context.SaveChangesAsync();
        Assert.That(await CareUnitPatientAccess.Query(context,"D1").CountAsync(),Is.Zero);
        var controller=new ReferralsController(context,new AuditLogWriter(context,new Actor()));
        controller.ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext {User=User("Avdelning","D1")}};
        var booking=await context.Bookings.SingleAsync();
        Assert.That((await controller.GetActions(booking.BookingId.ToString(),default)).Result,Is.TypeOf<ForbidResult>());
        Assert.That((await controller.RecordPatientAction(booking.BookingId.ToString(),new CreateReferralActionDto {ActionType=BookingSystem.Domain.Enums.ReferralActionType.PatientPaused,Comment="Test"},default)).Result,Is.TypeOf<ForbidResult>());
        Assert.That(await context.ReferralActions.CountAsync(),Is.Zero);
    }
    [Test]
    public async Task Newly_Created_Patient_Gets_Explicit_Relation_And_Survives_Log_Removal()
    {
        using var context=Context();
        var patient=await new CreatePatientCommandHandler(context,new Actor()).Handle(new CreatePatientCommand(new CreatePatientDto {FullName="Test",SocialSecurityNumber="Test"}),default);
        context.AuditLogs.RemoveRange(context.AuditLogs);await context.SaveChangesAsync();
        Assert.That(await CareUnitPatientAccess.Query(context,"D1").AnyAsync(p=>p.PatientId==patient.PatientId),Is.True);
        Assert.That(await CareUnitPatientAccess.Query(context,"D2").AnyAsync(),Is.False);
    }
    [Test]
    public async Task Transfer_Revokes_Previous_Department_Atomically()
    {
        using var context=Context();
        var role=new Role {RoleName="Avdelning"};context.Users.Add(new BookingSystem.Domain.Entities.User {Role=role,CareUnitId="D2"});
        context.Patients.Add(new Patient {PatientId=1});context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId="D1",Kind="department"});await context.SaveChangesAsync();
        var controller=new PatientsController(null!,null!,context);controller.ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext {User=User("Admin","")}};
        Assert.That(await controller.Transfer(1,new PatientsController.TransferPatientDto("D2","Överflyttning")),Is.TypeOf<NoContentResult>());
        Assert.That(await CareUnitPatientAccess.Query(context,"D1").AnyAsync(),Is.False);
        Assert.That(await CareUnitPatientAccess.Query(context,"D2").AnyAsync(),Is.True);
    }
    [Test]
    public async Task Department_Cannot_Grant_Itself_Access_Or_Transfer_Patients()
    {
        var controller=new PatientsController(null!,null!,null!);controller.ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext {User=User("Avdelning","D1")}};
        Assert.That(await controller.UpdateCareUnit(1,new PatientsController.PatientMembershipDto("D1","department",true,"Test")),Is.TypeOf<ForbidResult>());
        Assert.That(await controller.Transfer(1,new PatientsController.TransferPatientDto("D1","Test")),Is.TypeOf<ForbidResult>());
    }
    [Test]
    public void Migration_Generates_SQL_Without_Connecting_To_Database()
    {
        using var context=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var sql=context.GetService<IMigrator>().GenerateScript("20261006120000_AddDepartmentPriorityToBookings","20261011150000_AccountSessionVersion");
        Assert.That(sql,Does.Contain("CREATE TABLE [PatientCareUnits]"));
        Assert.That(sql,Does.Contain("ADD [TreatmentUnitId]"));
        Assert.That(sql,Does.Contain("ADD [SessionVersion]"));
    }
    [Test]
    public void Scoped_Reads_Translate_To_SQL_Server_Queries()
    {
        using var context=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var patients=CareUnitPatientAccess.Query(context,"T1","Behandlingsenhet").ToQueryString();
        var bookings=CareUnitPatientAccess.RestrictBookings(context,BookingAccess.Query(context.Bookings,"Behandlingsenhet","T1"),"Behandlingsenhet","T1").ToQueryString();
        Assert.That(patients,Does.Contain("PatientCareUnits"));
        Assert.That(bookings,Does.Contain("TreatmentUnitId"));
    }
    [Test]
    public async Task Assignment_Revokes_Old_Unit_And_Grants_Receiving_Unit()
    {
        using var context=Context();
        context.Patients.Add(new Patient {PatientId=1});
        context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId="T1",Kind="treatment"});
        var booking=new Booking {PatientId=1,DepartmentId="D1",TreatmentUnitId="T1"};context.Bookings.Add(booking);
        context.Users.Add(new BookingSystem.Domain.Entities.User {Role=new Role {RoleName="Behandlingsenhet"},CareUnitId="T2"});await context.SaveChangesAsync();
        var controller=new BookingController(null!,null!,context);controller.ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext {User=User("Admin","")}};
        Assert.That(await controller.AssignTreatmentUnit(booking.BookingId,new BookingController.AssignTreatmentUnitDto("T2")),Is.TypeOf<NoContentResult>());
        Assert.That(await CareUnitPatientAccess.Query(context,"T1","Behandlingsenhet").AnyAsync(),Is.False);
        Assert.That(await CareUnitPatientAccess.Query(context,"T2","Behandlingsenhet").AnyAsync(),Is.True);
        Assert.That(booking.DepartmentId,Is.EqualTo("D1"));
    }
    [Test]
    public async Task Another_Active_Referral_Preserves_Old_Treatment_Relation()
    {
        using var context=Context();context.Patients.Add(new Patient {PatientId=1});
        context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId="T1",Kind="treatment"});
        context.Bookings.Add(new Booking {PatientId=1,TreatmentUnitId="T1"});await context.SaveChangesAsync();
        await PatientUnitMemberships.RevokeUnusedTreatment(context,1,"T1");await context.SaveChangesAsync();
        Assert.That(await CareUnitPatientAccess.Query(context,"T1","Behandlingsenhet").AnyAsync(),Is.True);
    }
    [Test]
    public async Task Unassigned_And_Unknown_Units_Cannot_Be_Silently_Granted()
    {
        using var context=Context();context.Patients.Add(new Patient {PatientId=1});var booking=new Booking {PatientId=1,DepartmentId="D1"};context.Bookings.Add(booking);await context.SaveChangesAsync();
        var controller=new BookingController(null!,null!,context);controller.ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext {User=User("Admin","")}};
        Assert.That(await controller.AssignTreatmentUnit(booking.BookingId,new BookingController.AssignTreatmentUnitDto("Unknown")),Is.TypeOf<BadRequestObjectResult>());
        Assert.That(await context.PatientCareUnits.AnyAsync(),Is.False);
        controller.HttpContext.User=User("Avdelning","D1");
        Assert.That(await controller.AssignTreatmentUnit(booking.BookingId,new BookingController.AssignTreatmentUnitDto("T1")),Is.TypeOf<ForbidResult>());
    }
    private sealed class Actor:IAuditContext {public int? UserId=>1;public string? UserName=>"Test";public string? Role=>"Avdelning";public string? CareUnitId=>"D1";public string? IpAddress=>null;public string? RequestMethod=>"TEST";public string? RequestPath=>"/test";public string? Reason=>"Test";}
}
