// Backend purpose: Defines the write-side application flow for UpdateUserCommandHandler.
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Commands.UserCommands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly AppDbContext _context;

    public UpdateUserCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
        if (user == null) return false;

        if (!string.IsNullOrWhiteSpace(request.Dto.Fullname))
            user.Fullname = request.Dto.Fullname;

        if (!string.IsNullOrWhiteSpace(request.Dto.Email))
            user.Email = request.Dto.Email;

        if (!string.IsNullOrWhiteSpace(request.Dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Dto.Password);

        if (request.Dto.RoleId is > 0)
            user.RoleId = request.Dto.RoleId.Value;

        if (request.Dto.CareUnitId is not null)
            user.CareUnitId = string.IsNullOrWhiteSpace(request.Dto.CareUnitId) ? null : request.Dto.CareUnitId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

}
