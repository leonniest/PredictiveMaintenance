using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Auth;
using PredictiveMaintenance.Application.Interfaces;

namespace PredictiveMaintenance.Application.Services;

public interface IAuthService
{
    Task<UserProfileDto?> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IPredictiveMaintenanceDbContext db,
    IPasswordHasher passwordHasher) : IAuthService
{
    public async Task<UserProfileDto?> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Company)
            .SingleOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new UserProfileDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.Role,
            user.CompanyId,
            user.Company?.Name);
    }
}
