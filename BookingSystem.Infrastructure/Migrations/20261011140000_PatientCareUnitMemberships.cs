using System;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace BookingSystem.Infrastructure.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261011140000_PatientCareUnitMemberships")]
public partial class PatientCareUnitMemberships : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"PatientCareUnits", columns:table => new {
            PatientId=table.Column<int>(type:"int",nullable:false),
            UnitId=table.Column<string>(type:"nvarchar(120)",maxLength:120,nullable:false),
            Kind=table.Column<string>(type:"nvarchar(20)",maxLength:20,nullable:false),
            IsActive=table.Column<bool>(type:"bit",nullable:false),
            UpdatedAt=table.Column<DateTime>(type:"datetime2",nullable:false),
            Reason=table.Column<string>(type:"nvarchar(500)",maxLength:500,nullable:false)
        },constraints:table => {
            table.PrimaryKey("PK_PatientCareUnits",x=>new{x.PatientId,x.UnitId,x.Kind});
            table.ForeignKey("FK_PatientCareUnits_Patients_PatientId",x=>x.PatientId,"Patients","PatientId",onDelete:ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_PatientCareUnits_UnitId_Kind_IsActive","PatientCareUnits",new[]{"UnitId","Kind","IsActive"});
        migrationBuilder.Sql(@"
            INSERT INTO [PatientCareUnits] ([PatientId],[UnitId],[Kind],[IsActive],[UpdatedAt],[Reason])
            SELECT DISTINCT b.[PatientId],b.[DepartmentId],'department',1,SYSUTCDATETIME(),N'Migrerad avdelningsrelation'
            FROM [Bookings] b JOIN [Patients] p ON p.[PatientId]=b.[PatientId]
            WHERE b.[DepartmentId] IS NOT NULL AND LEN(b.[DepartmentId])>0;
            INSERT INTO [PatientCareUnits] ([PatientId],[UnitId],[Kind],[IsActive],[UpdatedAt],[Reason])
            SELECT DISTINCT b.[PatientId],b.[TreatmentUnitId],'treatment',1,SYSUTCDATETIME(),N'Migrerad behandlingstilldelning'
            FROM [Bookings] b JOIN [Patients] p ON p.[PatientId]=b.[PatientId]
            WHERE b.[TreatmentUnitId] IS NOT NULL AND LEN(b.[TreatmentUnitId])>0;
            INSERT INTO [PatientCareUnits] ([PatientId],[UnitId],[Kind],[IsActive],[UpdatedAt],[Reason])
            SELECT DISTINCT p.[PatientId],a.[CareUnitId],'department',1,SYSUTCDATETIME(),N'Migrerad patientskapandehändelse'
            FROM [AuditLogs] a JOIN [Patients] p ON p.[PatientId]=TRY_CONVERT(int,a.[EntityId])
            WHERE a.[EntityName]='Patient' AND a.[Action]='Added' AND a.[CareUnitId] IS NOT NULL AND LEN(a.[CareUnitId])>0
            AND a.[Role] IN (N'Avdelning',N'Remitterande avdelning',N'Vårdavdelning',N'Mottagning')
            AND NOT EXISTS (SELECT 1 FROM [PatientCareUnits] m WHERE m.[PatientId]=p.[PatientId] AND m.[UnitId]=a.[CareUnitId] AND m.[Kind]='department');
        ");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("PatientCareUnits");
}
