using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Application.Services;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Infrastructure.Persistence;

public sealed class SeedData(
    PredictiveMaintenanceDbContext db,
    IPasswordHasher passwordHasher,
    IAlertService alertService)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Companies.AnyAsync(cancellationToken))
        {
            return;
        }

        var atlas = Company("Atlas Access Systems", "Mara Janssen", "service@atlas-access.test", "Access automation reseller");
        var vector = Company("Vector Gate Partners", "Elias de Vries", "service@vector-gate.test", "Gate systems reseller");
        var orbit = Company("Orbit Entry Solutions", "Sofia Bakker", "service@orbit-entry.test", "Industrial entry reseller");

        db.Companies.AddRange(atlas, vector, orbit);
        db.ResellerSupportSettings.AddRange(
            Support(atlas, "support@atlas-access.test", "technicians@atlas-access.test", "+31 20 010 1100", 16),
            Support(vector, "support@vector-gate.test", "dispatch@vector-gate.test", "+31 30 020 2200", 24),
            Support(orbit, "support@orbit-entry.test", "fieldteam@orbit-entry.test", "+31 40 030 3300", 12));

        AddMachine(atlas, "Transit Hub Rotating Door RD-17", "Rotating door", "Zone A entrance", new DateOnly(2022, 4, 12), ControllerKind.Pcb, "DormaKaba", "PCB-RD900", "PCB-RD17-A", "4.7.2",
        [
            Part("Drive motor left wing", PartType.Motor, "SEW Eurodrive", "MX1200", new DateOnly(2025, 2, 3), new DateOnly(2025, 2, 3), 940, 1_080_000, 1_620_000, 61, true),
            Part("Door belt upper ring", PartType.Belt, "Gates", "PolyFlex-D", new DateOnly(2025, 7, 18), new DateOnly(2025, 7, 18), 210, 640_000, 640_000, 48, true),
            Part("Position relay bank A", PartType.Relay, "Finder", "55.34", new DateOnly(2024, 9, 10), new DateOnly(2024, 9, 10), 75, 710_000, 710_000, 45, false),
            Part("Presence sensor canopy", PartType.Sensor, "Sick", "PIR-Guard 4", new DateOnly(2024, 1, 5), null, 330, 980_000, 980_000, 41, false)
        ]);

        AddMachine(atlas, "Service Yard Security Gate GX-9", "Gate system", "Service perimeter east", new DateOnly(2021, 11, 22), ControllerKind.Plc, "Siemens", "S7-1500", "PLC-GX9-E", "2.9.4",
        [
            Part("Sliding gate traction motor", PartType.Motor, "ABB", "M3BP", new DateOnly(2024, 5, 8), new DateOnly(2024, 5, 8), 1280, 870_000, 1_305_000, 69, true),
            Part("Limit relay K4", PartType.Relay, "Omron", "G2R", new DateOnly(2023, 10, 1), null, 63, 840_000, 840_000, 53, true),
            Part("Chain tension sensor", PartType.Sensor, "IFM", "KG5071", new DateOnly(2022, 6, 1), null, 390, 1_220_000, 1_220_000, 42, false)
        ]);

        AddMachine(atlas, "Regional Clinic Sliding Door SD-06", "Sliding door", "Clinic reception", new DateOnly(2024, 6, 14), ControllerKind.Pcb, "Aster Controls", "PCB-SD410", "PCB-SD06-R", "2.4.8",
        [
            Part("Quiet drive motor", PartType.Motor, "VoltEdge", "QM650", new DateOnly(2025, 9, 4), new DateOnly(2025, 9, 4), 720, 420_000, 630_000, 49, false),
            Part("Linear carriage belt", PartType.Belt, "FlexLine", "LX-18", new DateOnly(2025, 9, 4), new DateOnly(2025, 9, 4), 170, 360_000, 360_000, 39, false),
            Part("Door closed relay", PartType.Relay, "CircuitNorth", "RN-22", new DateOnly(2024, 6, 14), null, 55, 580_000, 580_000, 44, false),
            Part("Threshold presence sensor", PartType.Sensor, "BeamWise", "BW-IR7", new DateOnly(2024, 6, 14), null, 290, 640_000, 640_000, 37, false)
        ]);

        AddMachine(vector, "Office Tower Rotating Door RD-03", "Rotating door", "Main lobby", new DateOnly(2023, 2, 4), ControllerKind.Pcb, "Record", "PCB-Rota 12", "PCB-NG-RD03", "3.6.9",
        [
            Part("Main drive motor", PartType.Motor, "Bonfiglioli", "BN71", new DateOnly(2023, 2, 4), null, 870, 1_020_000, 1_530_000, 58, true),
            Part("Drive belt lower track", PartType.Belt, "Optibelt", "Omega HP", new DateOnly(2025, 10, 21), new DateOnly(2025, 10, 21), 190, 280_000, 280_000, 37, false),
            Part("Safety sensor inner arc", PartType.Sensor, "Pepperl+Fuchs", "PIR20", new DateOnly(2024, 5, 1), null, 280, 760_000, 760_000, 39, false)
        ]);

        AddMachine(vector, "Parking Barrier PB-12", "Gate system", "Parking deck 2", new DateOnly(2020, 7, 16), ControllerKind.Plc, "Schneider", "Modicon M221", "PLC-PB12", "1.8.1",
        [
            Part("Barrier lift motor", PartType.Motor, "Nidec", "LiftPro 800", new DateOnly(2025, 1, 11), new DateOnly(2025, 1, 11), 760, 680_000, 1_020_000, 55, false),
            Part("Arm position relay", PartType.Relay, "Phoenix", "PLC-RSC", new DateOnly(2025, 3, 2), new DateOnly(2025, 3, 2), 58, 595_000, 595_000, 47, false),
            Part("Vehicle loop sensor", PartType.Sensor, "Carlo Gavazzi", "LD30", new DateOnly(2024, 8, 15), null, 310, 860_000, 860_000, 44, true)
        ]);

        AddMachine(vector, "Logistics Access Gate AG-18", "Gate system", "Distribution entrance", new DateOnly(2022, 12, 2), ControllerKind.Plc, "NorthRelay", "PLC-A18", "PLC-AG18", "3.2.0",
        [
            Part("Rack drive motor", PartType.Motor, "MotionForge", "MF920", new DateOnly(2024, 9, 20), new DateOnly(2024, 9, 20), 1080, 760_000, 1_140_000, 57, false),
            Part("Rack belt carrier", PartType.Belt, "ContiLine", "RG-55", new DateOnly(2024, 3, 7), new DateOnly(2024, 3, 7), 230, 520_000, 520_000, 45, true),
            Part("Access relay rail", PartType.Relay, "SignalWorks", "SW-R8", new DateOnly(2023, 1, 15), null, 70, 760_000, 760_000, 51, true),
            Part("Vehicle safety curtain", PartType.Sensor, "LineSight", "LS-CURTAIN", new DateOnly(2025, 4, 10), null, 430, 350_000, 350_000, 35, false)
        ]);

        AddMachine(orbit, "Warehouse Dock Gate DG-44", "Gate system", "Dock 44", new DateOnly(2022, 9, 30), ControllerKind.Plc, "Allen-Bradley", "CompactLogix", "PLC-DG44", "34.011",
        [
            Part("Dock gate motor", PartType.Motor, "Lenze", "m550", new DateOnly(2023, 12, 15), new DateOnly(2023, 12, 15), 1180, 1_240_000, 1_860_000, 72, true),
            Part("Gate belt assembly", PartType.Belt, "ContiTech", "Synchroforce", new DateOnly(2024, 4, 20), new DateOnly(2024, 4, 20), 250, 590_000, 590_000, 46, true),
            Part("Forklift safety sensor", PartType.Sensor, "Leuze", "RSL 420", new DateOnly(2024, 11, 12), null, 430, 460_000, 460_000, 36, false)
        ]);

        AddMachine(orbit, "Cold Storage Rotating Door RD-21", "Rotating door", "Cold storage entry", new DateOnly(2021, 3, 18), ControllerKind.Pcb, "Assa Abloy", "RD-Control X", "PCB-CS-RD21", "5.1.0",
        [
            Part("Insulated wing motor", PartType.Motor, "Nord", "SK 80", new DateOnly(2024, 2, 28), new DateOnly(2024, 2, 28), 990, 930_000, 1_395_000, 63, true),
            Part("Heated seal relay", PartType.Relay, "Finder", "40.52", new DateOnly(2024, 12, 8), null, 68, 510_000, 510_000, 56, true),
            Part("Temperature safety sensor", PartType.Sensor, "Sick", "WTB4", new DateOnly(2023, 9, 9), null, 360, 1_080_000, 1_080_000, 46, false)
        ]);

        AddMachine(orbit, "Campus Bike Gate BG-05", "Gate system", "Campus bike entry", new DateOnly(2025, 1, 24), ControllerKind.Pcb, "EntryLogic", "PCB-BG220", "PCB-BG05", "1.6.4",
        [
            Part("Compact gate motor", PartType.Motor, "VoltEdge", "CM480", new DateOnly(2025, 1, 24), null, 640, 510_000, 765_000, 52, false),
            Part("Barrier belt loop", PartType.Belt, "FlexLine", "BL-12", new DateOnly(2025, 1, 24), null, 145, 430_000, 430_000, 42, false),
            Part("Turnstile relay pair", PartType.Relay, "CircuitNorth", "RN-12", new DateOnly(2025, 1, 24), null, 48, 480_000, 480_000, 43, false),
            Part("Anti-tailgate sensor", PartType.Sensor, "BeamWise", "BW-AT3", new DateOnly(2025, 1, 24), null, 260, 520_000, 520_000, 38, false)
        ]);

        db.Users.AddRange(
            new AppUser { DisplayName = "Admin Showcase", Email = "admin@showcase.local", Role = UserRole.Admin, PasswordHash = passwordHasher.Hash("Admin123!") },
            new AppUser { DisplayName = "Atlas Reseller", Email = "reseller@atlas-access.test", Company = atlas, Role = UserRole.Reseller, PasswordHash = passwordHasher.Hash("Reseller123!") },
            new AppUser { DisplayName = "Vector Reseller", Email = "reseller@vector-gate.test", Company = vector, Role = UserRole.Reseller, PasswordHash = passwordHasher.Hash("Reseller123!") },
            new AppUser { DisplayName = "Orbit Reseller", Email = "reseller@orbit-entry.test", Company = orbit, Role = UserRole.Reseller, PasswordHash = passwordHasher.Hash("Reseller123!") });

        await db.SaveChangesAsync(cancellationToken);
        await alertService.EvaluateAsync(cancellationToken);
    }

    private static Company Company(string name, string contactName, string contactEmail, string industry)
        => new() { Name = name, ContactName = contactName, ContactEmail = contactEmail, Industry = industry };

    private static ResellerSupportSettings Support(Company company, string supportEmail, string technicianEmail, string phone, int slaHours)
        => new()
        {
            Company = company,
            SupportDepartmentEmail = supportEmail,
            TechnicianDispatchEmail = technicianEmail,
            EscalationPhone = phone,
            DefaultSlaHours = slaHours
        };

    private static SeedPart Part(string name, PartType type, string manufacturer, string model, DateOnly installedOn, DateOnly? replacedOn, decimal cost, int movements, int rotations, decimal averageTemperature, bool historicallyReplaced)
        => new(name, type, manufacturer, model, installedOn, replacedOn, cost, movements, rotations, averageTemperature, historicallyReplaced);

    private void AddMachine(Company company, string name, string type, string location, DateOnly installedOn, ControllerKind controllerKind, string controllerManufacturer, string controllerModel, string serial, string firmware, IReadOnlyList<SeedPart> seedParts)
    {
        var machine = new Machine
        {
            Company = company,
            Name = name,
            MachineType = type,
            Location = location,
            InstalledOn = installedOn,
            Controller = new ControllerUnit
            {
                Kind = controllerKind,
                Manufacturer = controllerManufacturer,
                Model = controllerModel,
                SerialNumber = serial,
                FirmwareVersion = firmware
            }
        };

        foreach (var seed in seedParts)
        {
            var part = new MachinePart
            {
                Name = seed.Name,
                Type = seed.Type,
                Manufacturer = seed.Manufacturer,
                Model = seed.Model,
                InstalledOn = seed.InstalledOn,
                LastReplacedOn = seed.ReplacedOn,
                ReplacementCost = seed.Cost
            };

            AddTelemetry(part, seed.Movements, seed.Rotations, seed.AverageTemperature);
            AddTechnicianHistory(part, seed.HistoricallyReplaced, seed.Movements, seed.Rotations, seed.AverageTemperature);
            machine.Controller.Parts.Add(part);
        }

        company.Machines.Add(machine);
    }

    private static void AddTelemetry(MachinePart part, int movements, int rotations, decimal averageTemperature)
    {
        var start = new DateOnly(2026, 1, 1);
        for (var i = 0; i < 12; i++)
        {
            var factor = 0.75m + (i * 0.045m);
            part.Telemetry.Add(new TelemetryDailySummary
            {
                Date = start.AddMonths(i),
                MovementCount = (int)(movements / 12m * factor),
                RotationCount = rotations == 0 ? 0 : (int)(rotations / 12m * factor),
                OperatingHours = 145 + i * 3,
                AverageTemperatureC = averageTemperature + ((i % 3) - 1) * 1.7m,
                MaxTemperatureC = averageTemperature + 7 + (i % 4)
            });
        }
    }

    private static void AddTechnicianHistory(MachinePart part, bool historicallyReplaced, int movements, int rotations, decimal averageTemperature)
    {
        part.TechnicianRecords.Add(new TechnicianRecord
        {
            WorkType = WorkType.Inspection,
            PerformedOn = new DateOnly(2025, 10, 15),
            TechnicianName = "Lena Vos",
            Notes = "Routine inspection. Lubrication and current draw verified.",
            LaborHours = 1.5m,
            PartCost = 0,
            MovementCountAtService = (int)(movements * 0.55),
            RotationCountAtService = (int)(rotations * 0.55),
            DaysSinceInstallAtService = 280,
            AverageTemperatureAtServiceC = averageTemperature - 3
        });

        if (historicallyReplaced)
        {
            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = WorkType.FailureReport,
                PerformedOn = new DateOnly(2024, 12, 18),
                TechnicianName = "Milan Peters",
                Notes = "Customer reported intermittent movement and rising noise before replacement window.",
                LaborHours = 0.75m,
                PartCost = 0,
                MovementCountAtService = (int)(HistoricalMovement(part.Type) * 0.88m),
                RotationCountAtService = rotations == 0 ? 0 : (int)(HistoricalMovement(part.Type) * 1.32m),
                DaysSinceInstallAtService = (int)(HistoricalDays(part.Type) * 0.88m),
                AverageTemperatureAtServiceC = HistoricalTemperature(part.Type) + 2
            });

            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = WorkType.Replacement,
                PerformedOn = new DateOnly(2025, 2, 3),
                TechnicianName = "Milan Peters",
                Notes = "Replacement completed after rising vibration and temperature profile.",
                LaborHours = 3.25m,
                PartCost = part.ReplacementCost,
                MovementCountAtService = HistoricalMovement(part.Type),
                RotationCountAtService = rotations == 0 ? 0 : HistoricalMovement(part.Type) * 3 / 2,
                DaysSinceInstallAtService = HistoricalDays(part.Type),
                AverageTemperatureAtServiceC = HistoricalTemperature(part.Type)
            });
        }

        if (averageTemperature >= 55)
        {
            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = WorkType.Repair,
                PerformedOn = new DateOnly(2026, 3, 8),
                TechnicianName = "Noor Smit",
                Notes = "Heat spike investigated. Cleaned vents and tightened terminal block.",
                LaborHours = 2.1m,
                PartCost = 45,
                MovementCountAtService = (int)(movements * 0.78),
                RotationCountAtService = (int)(rotations * 0.78),
                DaysSinceInstallAtService = 420,
                AverageTemperatureAtServiceC = averageTemperature
            });
        }

        AddHistoricalReplacementSamples(part, rotations);
        AddRecentWorkOrderSamples(part, movements, rotations, averageTemperature);
    }

    private static void AddHistoricalReplacementSamples(MachinePart part, int rotations)
    {
        var seed = StableSeed(part.Name);
        var baseMovement = HistoricalMovement(part.Type);
        var baseDays = HistoricalDays(part.Type);
        var baseTemperature = HistoricalTemperature(part.Type);

        for (var i = 0; i < 8; i++)
        {
            var variance = 0.84m + (((i + seed) % 7) * 0.055m);
            var monthOffset = (i * 2 + seed) % 12;
            var performedOn = new DateOnly(2025, 7, 12).AddMonths(monthOffset).AddDays((seed + i) % 11);
            var movementAtService = (int)(baseMovement * variance);
            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = WorkType.Replacement,
                PerformedOn = performedOn,
                TechnicianName = TechnicianName(i),
                Notes = $"Historical replacement sample for {part.Manufacturer} {part.Model}.",
                LaborHours = decimal.Round(2.2m + ((i + seed) % 5) * 0.35m, 2),
                PartCost = decimal.Round(part.ReplacementCost * (0.92m + ((i + seed) % 4) * 0.04m), 2),
                MovementCountAtService = movementAtService,
                RotationCountAtService = rotations == 0 ? movementAtService : (int)(movementAtService * 1.45m),
                DaysSinceInstallAtService = (int)(baseDays * (0.86m + (((i + seed) % 6) * 0.052m))),
                AverageTemperatureAtServiceC = baseTemperature + (((i + seed) % 5) - 2) * 1.6m
            });
        }
    }

    private static void AddRecentWorkOrderSamples(MachinePart part, int movements, int rotations, decimal averageTemperature)
    {
        var seed = StableSeed(part.Name);
        for (var i = 0; i < 4; i++)
        {
            var performedOn = new DateOnly(2025, 7, 5).AddMonths((i * 3 + seed) % 12).AddDays(seed % 9);
            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = i % 3 == 0 ? WorkType.Inspection : WorkType.Repair,
                PerformedOn = performedOn,
                TechnicianName = TechnicianName(seed + i),
                Notes = i % 3 == 0
                    ? "Preventive maintenance visit. Checked current draw, belt tension and relay response."
                    : "Corrective work order. Adjusted alignment, cleaned enclosure and verified controller events.",
                LaborHours = decimal.Round(1.1m + i * 0.45m, 2),
                PartCost = i % 3 == 0 ? 0 : 35 + (seed % 4) * 18,
                MovementCountAtService = (int)(movements * (0.30m + i * 0.15m)),
                RotationCountAtService = (int)(rotations * (0.30m + i * 0.15m)),
                DaysSinceInstallAtService = 120 + i * 90,
                AverageTemperatureAtServiceC = averageTemperature - 2 + i
            });
        }

        if (averageTemperature >= 50 || movements >= HistoricalMovement(part.Type) * 0.75m)
        {
            part.TechnicianRecords.Add(new TechnicianRecord
            {
                WorkType = WorkType.FailureReport,
                PerformedOn = new DateOnly(2026, 4, 18).AddDays(seed % 10),
                TechnicianName = TechnicianName(seed + 7),
                Notes = "Operator reported abnormal behavior before planned service window.",
                LaborHours = 0.6m,
                PartCost = 0,
                MovementCountAtService = (int)(movements * 0.88m),
                RotationCountAtService = (int)(rotations * 0.88m),
                DaysSinceInstallAtService = 510,
                AverageTemperatureAtServiceC = averageTemperature + 1
            });
        }
    }

    private static int StableSeed(string value)
        => value.Sum(c => c) % 17;

    private static string TechnicianName(int seed)
        => (seed % 5) switch
        {
            0 => "Lena Vos",
            1 => "Milan Peters",
            2 => "Noor Smit",
            3 => "Eva Kuiper",
            _ => "Sam Vermeer"
        };

    private static int HistoricalMovement(PartType type)
        => type switch
        {
            PartType.Motor => 1_130_000,
            PartType.Belt => 610_000,
            PartType.Relay => 790_000,
            PartType.Sensor => 1_260_000,
            _ => 900_000
        };

    private static int HistoricalDays(PartType type)
        => type switch
        {
            PartType.Motor => 725,
            PartType.Belt => 485,
            PartType.Relay => 620,
            PartType.Sensor => 830,
            _ => 650
        };

    private static decimal HistoricalTemperature(PartType type)
        => type switch
        {
            PartType.Motor => 66,
            PartType.Belt => 47,
            PartType.Relay => 53,
            PartType.Sensor => 42,
            _ => 52
        };

    private sealed record SeedPart(
        string Name,
        PartType Type,
        string Manufacturer,
        string Model,
        DateOnly InstalledOn,
        DateOnly? ReplacedOn,
        decimal Cost,
        int Movements,
        int Rotations,
        decimal AverageTemperature,
        bool HistoricallyReplaced);
}
