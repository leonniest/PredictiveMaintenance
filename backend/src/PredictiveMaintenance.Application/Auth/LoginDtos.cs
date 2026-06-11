using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string DisplayName,
    string Email,
    UserRole Role,
    Guid? CompanyId,
    string? CompanyName);
