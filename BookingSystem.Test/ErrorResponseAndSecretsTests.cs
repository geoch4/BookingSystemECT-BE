using System.Text.Json;
using API;
using BookingSystem.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware = API.ExceptionMiddleware.ExceptionMiddleware;

namespace BookingSystem.Test;

[TestFixture]
public class ErrorResponseAndSecretsTests
{
    // ---- Error responses ----

    [Test]
    public async Task UnexpectedError_OutsideDevelopment_HidesInternals()
    {
        var (status, json) = await Run(new InvalidOperationException("SELECT * FROM Patients WHERE SocialSecurityNumber = '19811218-9876'"), Environments.Production);

        Assert.That(status, Is.EqualTo(StatusCodes.Status500InternalServerError));
        Assert.That(json.GetProperty("message").GetString(), Is.EqualTo(Middleware.GenericErrorMessage));
        Assert.That(json.TryGetProperty("traceId", out var traceId) && !string.IsNullOrEmpty(traceId.GetString()), Is.True);
        var raw = json.GetRawText();
        Assert.That(raw, Does.Not.Contain("SELECT"));
        Assert.That(raw, Does.Not.Contain("19811218"));
        Assert.That(json.TryGetProperty("stackTrace", out _), Is.False);
        Assert.That(json.TryGetProperty("error", out _), Is.False);
    }

    [Test]
    public async Task UnexpectedError_InDevelopment_KeepsDetailsForTheDeveloper()
    {
        var (status, json) = await Run(new InvalidOperationException("boom"), Environments.Development);

        Assert.That(status, Is.EqualTo(StatusCodes.Status500InternalServerError));
        Assert.That(json.GetProperty("error").GetString(), Is.EqualTo("boom"));
        Assert.That(json.TryGetProperty("stackTrace", out _), Is.True);
    }

    [Test]
    public async Task AppException_IsReturnedAsBadRequestWithItsMessage()
    {
        var (status, json) = await Run(new AppException("Remissen är redan avslutad."), Environments.Production);

        Assert.That(status, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(json.GetProperty("message").GetString(), Is.EqualTo("Remissen är redan avslutad."));
    }

    [Test]
    public async Task ValidationException_IsBadRequestNotServerError()
    {
        var failure = new ValidationException(new[] { new ValidationFailure("FullName", "Namn krävs.") });

        var (status, json) = await Run(failure, Environments.Production);

        Assert.That(status, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(json.GetProperty("message").GetString(), Is.EqualTo("Namn krävs."));
        Assert.That(json.GetProperty("errors").GetProperty("FullName")[0].GetString(), Is.EqualTo("Namn krävs."));
    }

    private static async Task<(int Status, JsonElement Json)> Run(Exception exception, string environmentName)
    {
        var environment = new Moq.Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        var middleware = new Middleware(_ => throw exception, NullLogger<Middleware>.Instance, environment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    // ---- Secrets ----

    private static IConfiguration Config(params (string Key, string Value)[] items) =>
        new ConfigurationBuilder().AddInMemoryCollection(items.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    private const string StrongKey = "a-strong-test-key-that-is-at-least-32-bytes-long";

    [Test]
    public void Production_RequiresAJwtKey() =>
        Assert.Throws<InvalidOperationException>(() => ConfigurationSafety.EnsureJwtKey(Config(), development: false));

    [Test]
    public void Development_GeneratesAnInMemoryKeyWhenMissing()
    {
        var config = Config();

        var generated = ConfigurationSafety.EnsureJwtKey(config, development: true);

        Assert.That(generated, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetByteCount(config[ConfigurationSafety.JwtKeyPath]!),
            Is.GreaterThanOrEqualTo(ConfigurationSafety.MinimumJwtKeyBytes));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void TheKeyThatWasCommittedToGit_IsRejectedEverywhere(bool development) =>
        Assert.Throws<InvalidOperationException>(() => ConfigurationSafety.EnsureJwtKey(
            Config((ConfigurationSafety.JwtKeyPath, "SuperHemligOchLångNyckelSomIngenVet123!")), development));

    [Test]
    public void ShortKey_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => ConfigurationSafety.EnsureJwtKey(
            Config((ConfigurationSafety.JwtKeyPath, "too-short")), development: false));

    [Test]
    public void ConfiguredStrongKey_IsKept()
    {
        var config = Config((ConfigurationSafety.JwtKeyPath, StrongKey));

        Assert.That(ConfigurationSafety.EnsureJwtKey(config, development: false), Is.False);
        Assert.That(config[ConfigurationSafety.JwtKeyPath], Is.EqualTo(StrongKey));
    }

    [Test]
    public void Production_RequiresAConnectionString()
    {
        Assert.Throws<InvalidOperationException>(() => ConfigurationSafety.RequireConnectionString(Config(), development: false));
        Assert.DoesNotThrow(() => ConfigurationSafety.RequireConnectionString(Config(), development: true));
        Assert.DoesNotThrow(() => ConfigurationSafety.RequireConnectionString(
            Config(("ConnectionStrings:DefaultConnection", "Server=sql;Database=ECT")), development: false));
    }

    [Test]
    public void CommittedAppSettings_ContainNoSecrets()
    {
        var path = FindRepoFile(Path.Combine("API", "appsettings.json"));
        var text = File.ReadAllText(path);

        Assert.That(text, Does.Not.Contain("SecretKey"));
        Assert.That(text, Does.Not.Contain("ConnectionStrings"));
    }

    private static string FindRepoFile(string relative)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate)) return candidate;
        }
        Assert.Fail($"Hittade inte {relative} uppåt från testkatalogen.");
        return string.Empty;
    }
}
