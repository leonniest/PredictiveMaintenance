namespace PredictiveMaintenance.Application.Support;

public sealed record SupportSettingsDto(
    Guid CompanyId,
    string CompanyName,
    string SupportDepartmentEmail,
    string TechnicianDispatchEmail,
    string EscalationPhone,
    int DefaultSlaHours,
    DateTimeOffset UpdatedAt);

public sealed record UpdateSupportSettingsRequest(
    Guid CompanyId,
    string SupportDepartmentEmail,
    string TechnicianDispatchEmail,
    string EscalationPhone,
    int DefaultSlaHours);

public sealed record DispatchTechnicianRequest(
    Guid MachineId,
    Guid? AlertId,
    string Note);

public sealed record DispatchTechnicianResultDto(
    Guid MachineId,
    Guid? AlertId,
    IReadOnlyList<string> Recipients,
    DateTimeOffset SentAt);
