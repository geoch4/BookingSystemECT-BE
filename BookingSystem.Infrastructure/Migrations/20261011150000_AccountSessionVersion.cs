using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace BookingSystem.Infrastructure.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261011150000_AccountSessionVersion")]
public partial class AccountSessionVersion:Migration
{
    protected override void Up(MigrationBuilder m) {
        m.AddColumn<string>("SessionVersion","Users",type:"nvarchar(32)",maxLength:32,nullable:false,defaultValue:"");
        m.Sql("UPDATE [Users] SET [SessionVersion]=REPLACE(CONVERT(nvarchar(36),NEWID()),'-','');");
    }
    protected override void Down(MigrationBuilder m)=>m.DropColumn("SessionVersion","Users");
}
