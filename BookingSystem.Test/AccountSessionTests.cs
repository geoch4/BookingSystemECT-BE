using System.Security.Claims;
using API;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BookingSystem.Test;
public class AccountSessionTests
{
    [TestCase("role")][TestCase("unit")][TestCase("password")][TestCase("email")][TestCase("delete")]
    public async Task Security_Change_Immediately_Invalidates_Previously_Issued_Claims(string change)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user=new User {Role=new Role {RoleName="Avdelning"},CareUnitId="D1"};db.Users.Add(user);await db.SaveChangesAsync();
        var claims=Principal(user);var old=user.SessionVersion;
        Assert.That(await AccountSessionValidator.IsCurrent(db,claims),Is.True);
        switch(change){case "role":user.Role=new Role {RoleName="Admin"};break;case "unit":user.CareUnitId="D2";break;case "password":user.PasswordHash="new hash";break;case "email":user.Email="new@example.test";break;case "delete":user.IsDeleted=true;break;}
        await db.SaveChangesAsync();
        Assert.That(user.SessionVersion,Is.Not.EqualTo(old));
        Assert.That(await AccountSessionValidator.IsCurrent(db,claims),Is.False);
        if(change!="delete")Assert.That(await AccountSessionValidator.IsCurrent(db,Principal(user)),Is.True);
    }
    [Test]
    public async Task Restoring_The_Old_Unit_Does_Not_Reactivate_The_Old_Token()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user=new User {Role=new Role {RoleName="Avdelning"},CareUnitId="D1"};db.Users.Add(user);await db.SaveChangesAsync();var old=Principal(user);
        user.CareUnitId="D2";db.SaveChanges();user.CareUnitId="D1";await db.SaveChangesAsync();
        Assert.That(await AccountSessionValidator.IsCurrent(db,old),Is.False);
    }
    [Test]
    public async Task Missing_Version_Is_Denied_And_Profile_Edit_Preserves_Session()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user=new User {Role=new Role {RoleName="Admin"}};db.Users.Add(user);await db.SaveChangesAsync();var current=Principal(user);
        user.Fullname="Updated name";await db.SaveChangesAsync();Assert.That(await AccountSessionValidator.IsCurrent(db,current),Is.True);
        var old=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),new Claim(ClaimTypes.Role,"Admin")},"Test"));
        Assert.That(await AccountSessionValidator.IsCurrent(db,old),Is.False);
    }
    [Test]
    public async Task Renaming_And_Restoring_A_Role_Cannot_Reactivate_Old_Tokens()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user=new User {Role=new Role {RoleName="Avdelning"},CareUnitId="D1"};db.Users.Add(user);await db.SaveChangesAsync();var old=Principal(user);
        user.Role.RoleName="Mottagning";await db.SaveChangesAsync();user.Role.RoleName="Avdelning";await db.SaveChangesAsync();
        Assert.That(await AccountSessionValidator.IsCurrent(db,old),Is.False);
    }
    public static ClaimsPrincipal Principal(User user)=>new(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),new Claim(ClaimTypes.Role,user.Role.RoleName ?? ""),new Claim("care_unit",user.CareUnitId??""),new Claim("session_version",user.SessionVersion)},"Test"));
}
