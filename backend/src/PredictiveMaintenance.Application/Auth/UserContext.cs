using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Auth;

public sealed record UserContext(Guid UserId, UserRole Role, Guid? CompanyId, string Email);
