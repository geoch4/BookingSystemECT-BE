using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
﻿// Backend purpose: Exposes HTTP endpoints for the AuthController backend feature.
using BookingSystem.Application.Commands.UserCommands.CreateUser;
using BookingSystem.Application.Commands.UserCommands.LogInUser;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromServices] AppDbContext context, CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            var version = User.FindFirstValue("session_version");
            var nextVersion = Guid.NewGuid().ToString("N");
            // Compare-and-swap prevents a delayed logout from revoking a newer session.
            await context.Users.Where(u => u.UserId == userId && u.SessionVersion == version)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SessionVersion, nextVersion), cancellationToken);
            return NoContent();
        }

        [EnableRateLimiting(LoginRateLimiting.Policy)]
        [HttpPost("login")]
        public async Task<ActionResult<OperationResult<string>>> Login([FromBody] LoginDto dto)
        {
            var result = await _mediator.Send(new LogInUserCommand(dto));

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [Authorize(Policy = AccessPolicies.OnlyAdmin)]
        [HttpPost("register")]
        public async Task<ActionResult<OperationResult<int>>> Register([FromBody] CreateUserDto dto)
        {
            var result = await _mediator.Send(new CreateUserCommand(dto));

            if (!result.Success)
                return BadRequest(result);

            // Another option is to return Created(...) with the OperationResult payload.
            return Ok(result);
        }
    }
}
