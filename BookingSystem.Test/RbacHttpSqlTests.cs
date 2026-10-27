using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API;
using API.Controllers;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Queries.GetAllUsers;
using BookingSystem.Application.Service.Implementation;
using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace BookingSystem.Test;
[NonParallelizable]
public class RbacHttpSqlTests
{
    private sealed class SqlTestSchema(DbContextOptions<AppDbContext> options):AppDbContext(options) { protected override void OnModelCreating(ModelBuilder modelBuilder) { base.OnModelCreating(modelBuilder); modelBuilder.Entity<Patient>().ToTable("Patients",t=>t.ExcludeFromMigrations(false)); } }
    [Test,Explicit("Creates and removes only a uniquely named RbacTest_ database on the selected local SQL instance")]
    public async Task Signed_JWT_HTTP_And_SQL_Enforce_Roles_Scopes_And_Revocation()
    {
        var database="RbacTest_"+Guid.NewGuid().ToString("N");
        var server=Environment.GetEnvironmentVariable("RBAC_SQL_SERVER") ?? @".\SQLEXPRESS";
        var connection=$"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10";
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> { ["Jwt:SecretKey"]=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N"),["Jwt:Issuer"]="rbac-test",["Jwt:Audience"]="rbac-test" });
        builder.Services.AddDbContext<AppDbContext>(o=>o.UseSqlServer(connection));
        builder.Services.AddHttpContextAccessor();builder.Services.AddScoped<IAuditContext,HttpAuditContext>();builder.Services.AddScoped<AuditLogWriter>();
        builder.Services.AddMediatR(c=>c.RegisterServicesFromAssembly(typeof(GetAllUsersQuery).Assembly));
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddAccountJwtAuthentication(builder.Configuration).AddAccountAuthorization().AddLoginRateLimiting();
        builder.Services.AddControllers().AddApplicationPart(typeof(ReferralsController).Assembly);
        await using var app=builder.Build();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();app.MapControllers();
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            using (var bootstrap = new SqlTestSchema(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options)) await bootstrap.Database.EnsureCreatedAsync();
            var department=new User {Email="department@rbac.test",Fullname="Synthetic department",PasswordHash=BCrypt.Net.BCrypt.HashPassword("SyntheticTestPassword!"),Role=new Role {RoleName="Avdelning"},CareUnitId="D1"};
            var treatment=new User {Role=new Role {RoleName="Behandlingsenhet"},CareUnitId="T1"};
            var medical=new User {Role=new Role {RoleName="Medicinskt ansvarig"},CareUnitId="T1"};
            var admin=new User {Role=new Role {RoleName="Admin"}};db.Users.AddRange(department,treatment,medical,admin);
            var p1=new Patient {FullName="Synthetic A",SocialSecurityNumber="TEST-A"};var p2=new Patient {FullName="Synthetic B",SocialSecurityNumber="TEST-B"};db.Patients.AddRange(p1,p2);await db.SaveChangesAsync();
            db.PatientCareUnits.AddRange(new PatientCareUnit {PatientId=p1.PatientId,UnitId="D1",Kind="department"},new PatientCareUnit {PatientId=p1.PatientId,UnitId="T1",Kind="treatment"},new PatientCareUnit {PatientId=p2.PatientId,UnitId="D2",Kind="department"},new PatientCareUnit {PatientId=p2.PatientId,UnitId="T2",Kind="treatment"});
            var own=new Booking {PatientId=p1.PatientId,DepartmentId="D1",TreatmentUnitId="T1",Date=new DateTime(2026,10,11,9,0,0),CreatedByUserId=admin.UserId};
            var other=new Booking {PatientId=p2.PatientId,DepartmentId="D2",TreatmentUnitId="T2",Date=new DateTime(2026,10,11,10,0,0),CreatedByUserId=admin.UserId};db.Bookings.AddRange(own,other);await db.SaveChangesAsync();
            await app.StartAsync();var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http=new HttpClient {BaseAddress=new Uri(address)};
            var auth=scope.ServiceProvider.GetRequiredService<AuthService>();
            string Token(User user)=>auth.GenerateJwtToken(user.UserId,user.Fullname,user.Role.RoleName,user.CareUnitId,user.SessionVersion);
            void As(string token)=>http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
            async Task Status(string url,HttpStatusCode expected){using var response=await http.GetAsync(url);Assert.That(response.StatusCode,Is.EqualTo(expected),url);}
            await Status("/api/referrals/queue",HttpStatusCode.Unauthorized);
            using(var badLogin=await http.PostAsJsonAsync("/api/auth/login",new {email=department.Email,password="Incorrect"}))Assert.That(badLogin.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
            using var login=await http.PostAsJsonAsync("/api/auth/login",new {email=department.Email,password="SyntheticTestPassword!"});Assert.That(login.StatusCode,Is.EqualTo(HttpStatusCode.OK));
            var result=await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var deptToken=result.GetProperty("data").GetString()!;As(deptToken);
            await Status("/api/referrals/queue",HttpStatusCode.Forbidden);
            await Status($"/api/referrals/{own.BookingId}/actions",HttpStatusCode.OK);
            await Status($"/api/referrals/{other.BookingId}/actions",HttpStatusCode.Forbidden);
            var treatmentToken=Token(treatment);As(treatmentToken);
            var queue=await http.GetFromJsonAsync<BookingSystem.Application.DTO.ReferralDto[]>("/api/referrals/queue");Assert.That(queue!.Select(r=>r.BookingId),Is.EqualTo(new[]{own.BookingId}));
            using(var denied=await http.PostAsJsonAsync($"/api/referrals/{own.BookingId}/actions/prioritize",new {priority=2,comment="Synthetic test"}))Assert.That(denied.StatusCode,Is.EqualTo(HttpStatusCode.Forbidden));
            await Status($"/api/patients/{p2.PatientId}",HttpStatusCode.Forbidden);
            As(Token(medical));using(var allowed=await http.PostAsJsonAsync($"/api/referrals/{own.BookingId}/actions/prioritize",new {priority=2,comment="Synthetic test"}))Assert.That(allowed.StatusCode,Is.EqualTo(HttpStatusCode.OK));
            As(Token(admin));await Status("/api/user",HttpStatusCode.OK);
            var relation=await db.PatientCareUnits.FindAsync(p1.PatientId,"D1","department");relation!.IsActive=false;await db.SaveChangesAsync();
            As(deptToken);await Status($"/api/referrals/{own.BookingId}/actions",HttpStatusCode.Forbidden);
            treatment.CareUnitId="T2";await db.SaveChangesAsync();As(treatmentToken);await Status("/api/referrals/queue",HttpStatusCode.Unauthorized);
            var refreshed=Token(treatment);As(refreshed);await Status("/api/referrals/queue",HttpStatusCode.OK);
            treatment.IsDeleted=true;await db.SaveChangesAsync();await Status("/api/referrals/queue",HttpStatusCode.Unauthorized);
            As(Token(admin));
            using (var logout = await http.PostAsync("/api/auth/logout", null)) Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await Status("/api/user", HttpStatusCode.Unauthorized);
            await db.Entry(admin).ReloadAsync();
            As(Token(admin)); await Status("/api/user", HttpStatusCode.OK);
            As(deptToken+"tampered");await Status("/api/referrals/my",HttpStatusCode.Unauthorized);
            if (Environment.GetEnvironmentVariable("RBAC_RUN_BROWSER")=="1")
            {
                var frontend=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../BookingSystem-FE"));
                var start=new System.Diagnostics.ProcessStartInfo("node") {WorkingDirectory=frontend,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                start.ArgumentList.Add(Path.Combine(frontend,"node_modules/@playwright/test/cli.js"));
                start.ArgumentList.Add("test");start.ArgumentList.Add("e2e/rbac-live-api.spec.ts");start.ArgumentList.Add("--workers=1");
                start.Environment["RBAC_LIVE_API"]=address;start.Environment["RBAC_LIVE_TOKEN"]=Token(medical);
                using var process=System.Diagnostics.Process.Start(start)!;
                var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                Assert.That(process.ExitCode,Is.Zero,(await stdout)+"\n"+(await stderr));
            }
        }
        finally
        {
            await app.StopAsync();
            if (!database.StartsWith("RbacTest_",StringComparison.Ordinal) || database.Length!=41) throw new InvalidOperationException("Unsafe cleanup target");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
