using API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BookingSystem.Test;
public class DeploymentSafetyTests
{
    private static IConfiguration Config(params (string Key,string Value)[] items)=>new ConfigurationBuilder().AddInMemoryCollection(items.ToDictionary(x=>x.Key,x=>(string?)x.Value)).Build();
    [Test] public async Task Defaults_Do_Not_Touch_Database_Or_Seed_Data()
    {
        var settings=DeploymentSafety.Read(Config(),true);
        Assert.That(settings.ApplyMigrations,Is.False); Assert.That(settings.SeedDemoData,Is.False);
        await settings.Initialize(new ServiceCollection().BuildServiceProvider());
    }
    [Test] public void Production_Requires_Explicit_Https_Origins()=>Assert.Throws<InvalidOperationException>(()=>DeploymentSafety.Read(Config(),false));
    [TestCase("*")] [TestCase("http://pilot.example")] [TestCase("https://pilot.example/path")] [TestCase("https://user:pass@pilot.example")]
    public void Production_Rejects_Unsafe_Origins(string origin)=>Assert.Throws<InvalidOperationException>(()=>DeploymentSafety.Read(Config(("Cors:AllowedOrigins:0",origin)),false));
    [Test] public void Demo_Seeding_Cannot_Be_Enabled_In_Production()=>Assert.Throws<InvalidOperationException>(()=>DeploymentSafety.Read(Config(("Database:SeedDemoData","true"),("Cors:AllowedOrigins:0","https://pilot.example")),false));
    [Test] public void Explicit_Development_OptIn_Is_Respected()
    {
        var settings=DeploymentSafety.Read(Config(("Database:SeedDemoData","true"),("Database:ApplyMigrationsOnStartup","true")),true);
        Assert.That(settings.SeedDemoData && settings.ApplyMigrations,Is.True);
    }
    [Test] public void Production_Origin_Is_Kept_Without_Database_Changes()
    {
        var settings=DeploymentSafety.Read(Config(("Cors:AllowedOrigins:0","https://pilot.example")),false);
        Assert.That(settings.AllowedOrigins,Is.EqualTo(new[]{"https://pilot.example"})); Assert.That(settings.ApplyMigrations,Is.False);
    }
}
