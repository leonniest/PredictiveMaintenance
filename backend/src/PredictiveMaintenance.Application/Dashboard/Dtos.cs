using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int CompanyCount,
    int MachineCount,
    int PartCount,
    int OpenAlertCount,
    int CriticalAlertCount,
    IReadOnlyList<ResellerRiskDto> ResellersAtRisk,
    IReadOnlyList<AlertDto> RecentAlerts);

public sealed record ResellerRiskDto(
    Guid CompanyId,
    string CompanyName,
    int OpenAlerts,
    int CriticalAlerts,
    DateOnly EarliestDueDate,
    int AverageAlertHealthScore,
    int FleetHealthScore);

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string ContactName,
    string ContactEmail,
    string Industry,
    int MachineCount,
    int OpenAlertCount,
    int FleetHealthScore,
    IReadOnlyList<MachineDto> Machines);

public sealed record MachineDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string MachineType,
    string Location,
    DateOnly InstalledOn,
    ControllerDto Controller,
    int OpenAlertCount,
    int HealthScore);

public sealed record ControllerDto(
    Guid Id,
    ControllerKind Kind,
    string Manufacturer,
    string Model,
    string SerialNumber,
    string FirmwareVersion,
    IReadOnlyList<PartDto> Parts);

public sealed record PartDto(
    Guid Id,
    string Name,
    PartType Type,
    string Manufacturer,
    string Model,
    DateOnly InstalledOn,
    DateOnly? LastReplacedOn,
    decimal ReplacementCost,
    int TotalMovements,
    int TotalRotations,
    decimal AverageTemperatureC,
    PredictionDto Prediction);

public sealed record PredictionDto(
    int HealthScore,
    DateOnly PredictedDueDate,
    AlertSeverity Severity,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    decimal MovementRatio,
    decimal AgeRatio,
    decimal TemperatureRatio,
    int ExpectedMovementsToReplacement,
    int ExpectedDaysToReplacement,
    decimal ExpectedTemperatureAtReplacementC);

public sealed record AlertDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    Guid MachineId,
    string MachineName,
    Guid PartId,
    string PartName,
    PartType PartType,
    AlertSeverity Severity,
    AlertStatus Status,
    DateTimeOffset CreatedAt,
    DateOnly PredictedDueDate,
    int HealthScore,
    string Message,
    string? NotificationChannel,
    DateTimeOffset? NotifiedAt);

public sealed record PartLifetimeDto(
    PartType PartType,
    int ReplacementSamples,
    int AverageMovementsToReplacement,
    int AverageDaysToReplacement,
    decimal AverageTemperatureAtReplacementC,
    string LongestLastingPart,
    IReadOnlyList<PartModelLifetimeDto> Models);

public sealed record PartModelLifetimeDto(
    string Manufacturer,
    string Model,
    string PartName,
    int ReplacementSamples,
    int AverageMovementsToReplacement,
    int AverageDaysToReplacement,
    decimal AverageTemperatureAtReplacementC,
    int BestDaysToReplacement,
    int LowestDaysToReplacement);

public sealed record FailureTrendDto(
    string Month,
    int Inspections,
    int Repairs,
    int Replacements,
    int FailureReports,
    int TotalRecords);
