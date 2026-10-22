using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;
namespace API;

public sealed record DeploymentSafety(bool ApplyMigrations, bool SeedDemoData, string[] AllowedOrigins)
{
    public static DeploymentSafety Read(IConfiguration config, bool development)
    {
        var migrate=config.GetValue<bool>("Database:ApplyMigrationsOnStartup");
        var seed=config.GetValue<bool>("Database:SeedDemoData");
        if(seed && !development) throw new InvalidOperationException("Demo seeding is only permitted in Development.");
        var origins=config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if(origins.Length==0 && development) origins=["http://localhost:5173","http://127.0.0.1:5173"];
        if(origins.Length==0 || origins.Any(o=>!Uri.TryCreate(o,UriKind.Absolute,out var uri)
            || (uri.Scheme!="http" && uri.Scheme!="https") || uri.AbsolutePath!="/"
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo) || (!development && uri.Scheme!="https")))
            throw new InvalidOperationException("Configure explicit Cors:AllowedOrigins (HTTPS outside Development).");
        return new(migrate,seed,origins);
    }

    public async Task Initialize(IServiceProvider services, CancellationToken cancellationToken=default)
    {
        if(!ApplyMigrations && !SeedDemoData) return;
        using var scope=services.CreateScope();
        if(ApplyMigrations) await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
        if(SeedDemoData) scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedData();
    }
}
