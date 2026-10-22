using API;
using BookingSystem.Application.Service.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
namespace BookingSystem.Test;
public class LoginSafetyTests
{
    [TestCase(null)] [TestCase("")] [TestCase("legacy plain value")] [TestCase("$2a$12$broken")]
    public void Invalid_Hash_Denies_Login_Without_Server_Error(string? hash)=>Assert.That(PasswordVerification.Matches("synthetic-password",hash),Is.False);
    [Test] public void Correct_And_Incorrect_Passwords_Are_Distinguished()
    {
        var hash=BCrypt.Net.BCrypt.HashPassword("synthetic-password");
        Assert.That(PasswordVerification.Matches("synthetic-password",hash),Is.True);
        Assert.That(PasswordVerification.Matches("incorrect",hash),Is.False);
    }
    [Test] public async Task Repeated_Login_Requests_Receive_429_While_Other_Routes_Remain_Available()
    {
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddLoginRateLimiting();
        await using var app=builder.Build();app.UseRouting();app.UseRateLimiter();
        app.MapPost("/login",()=>Microsoft.AspNetCore.Http.Results.Unauthorized()).RequireRateLimiting(LoginRateLimiting.Policy);
        app.MapGet("/other",()=>"ok");await app.StartAsync();
        using var http=new HttpClient {BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())};
        try {
            for(var i=0;i<10;i++) {using var response=await http.PostAsync("/login",null);Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));}
            using var blocked=await http.PostAsync("/login",null);Assert.That(blocked.StatusCode,Is.EqualTo(HttpStatusCode.TooManyRequests));
            using var other=await http.GetAsync("/other");Assert.That(other.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        } finally {await app.StopAsync();}
    }
}
