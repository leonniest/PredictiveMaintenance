using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PredictiveMaintenance.Application;
using PredictiveMaintenance.Application.Assistant;
using PredictiveMaintenance.Application.Auth;
using PredictiveMaintenance.Application.Services;
using PredictiveMaintenance.Application.Support;
using PredictiveMaintenance.Domain.Enums;
using PredictiveMaintenance.Infrastructure;
using PredictiveMaintenance.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AlertEvaluationHostedService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a JWT from /api/auth/login."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [
            new OpenApiSecuritySchemeReference("Bearer", null)
        ] = []
    });
});

var app = builder.Build();

await SeedDatabaseWithRetryAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    IAuthService authService,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var user = await authService.ValidateCredentialsAsync(request, cancellationToken);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var options = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    var expires = DateTimeOffset.UtcNow.AddHours(options.ExpiresHours);
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Role, user.Role.ToString()),
        new("role", user.Role.ToString())
    };

    if (user.CompanyId is { } companyId)
    {
        claims.Add(new Claim("companyId", companyId.ToString()));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    var token = new JwtSecurityToken(
        issuer: options.Issuer,
        audience: options.Audience,
        claims: claims,
        expires: expires.UtcDateTime,
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user));
}).AllowAnonymous();

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/me", (ClaimsPrincipal principal) =>
{
    var user = principal.ToUserContext();
    return Results.Ok(user);
});

api.MapGet("/dashboard/summary", async (ClaimsPrincipal principal, IDashboardService service, CancellationToken ct)
    => Results.Ok(await service.GetSummaryAsync(principal.ToUserContext(), ct)));

api.MapGet("/companies", async (ClaimsPrincipal principal, IDashboardService service, CancellationToken ct)
    => Results.Ok(await service.GetCompaniesAsync(principal.ToUserContext(), ct)));

api.MapGet("/companies/{companyId:guid}", async (Guid companyId, ClaimsPrincipal principal, IDashboardService service, CancellationToken ct) =>
{
    var company = await service.GetCompanyAsync(principal.ToUserContext(), companyId, ct);
    return company is null ? Results.NotFound() : Results.Ok(company);
});

api.MapGet("/machines/{machineId:guid}", async (Guid machineId, ClaimsPrincipal principal, IDashboardService service, CancellationToken ct) =>
{
    var machine = await service.GetMachineAsync(principal.ToUserContext(), machineId, ct);
    return machine is null ? Results.NotFound() : Results.Ok(machine);
});

api.MapGet("/alerts", async (ClaimsPrincipal principal, IDashboardService service, CancellationToken ct)
    => Results.Ok(await service.GetAlertsAsync(principal.ToUserContext(), ct)));

api.MapPost("/alerts/evaluate", async (IAlertService service, CancellationToken ct) =>
    Results.Ok(new { Created = await service.EvaluateAsync(ct) })).RequireAuthorization("AdminOnly");

api.MapPost("/alerts/{alertId:guid}/acknowledge", async (Guid alertId, IAlertService service, CancellationToken ct) =>
    await service.AcknowledgeAsync(alertId, ct) ? Results.NoContent() : Results.NotFound());

api.MapPost("/alerts/{alertId:guid}/notify", async (Guid alertId, IAlertService service, CancellationToken ct) =>
    await service.NotifyAsync(alertId, ct) ? Results.NoContent() : Results.NotFound());

api.MapGet("/support/settings", async (ClaimsPrincipal principal, ISupportService service, CancellationToken ct)
    => Results.Ok(await service.GetSettingsAsync(principal.ToUserContext(), ct)));

api.MapPut("/support/settings", async (UpdateSupportSettingsRequest request, ClaimsPrincipal principal, ISupportService service, CancellationToken ct) =>
{
    var result = await service.UpdateSettingsAsync(principal.ToUserContext(), request, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

api.MapPost("/support/dispatch", async (DispatchTechnicianRequest request, ClaimsPrincipal principal, ISupportService service, CancellationToken ct) =>
{
    var result = await service.DispatchTechnicianAsync(principal.ToUserContext(), request, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

api.MapGet("/assistant/faq", (IAssistantService service)
    => Results.Ok(service.GetFaqPrompts()));

api.MapGet("/assistant/sessions", async (ClaimsPrincipal principal, IAssistantService service, CancellationToken ct)
    => Results.Ok(await service.GetSessionsAsync(principal.ToUserContext(), ct)));

api.MapPost("/assistant/sessions", async (CreateAssistantSessionRequest request, ClaimsPrincipal principal, IAssistantService service, CancellationToken ct)
    => Results.Ok(await service.CreateSessionAsync(principal.ToUserContext(), request, ct)));

api.MapGet("/assistant/sessions/{sessionId:guid}", async (Guid sessionId, ClaimsPrincipal principal, IAssistantService service, CancellationToken ct) =>
{
    var result = await service.GetSessionAsync(principal.ToUserContext(), sessionId, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

api.MapPost("/assistant/sessions/{sessionId:guid}/messages", async (Guid sessionId, SendAssistantMessageRequest request, ClaimsPrincipal principal, IAssistantService service, CancellationToken ct) =>
{
    var result = await service.SendMessageAsync(principal.ToUserContext(), sessionId, request, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

api.MapGet("/analytics/part-lifetimes", async (ClaimsPrincipal principal, IDashboardService service, CancellationToken ct)
    => Results.Ok(await service.GetPartLifetimesAsync(principal.ToUserContext(), ct)));

api.MapGet("/analytics/failure-trends", async (ClaimsPrincipal principal, IDashboardService service, CancellationToken ct)
    => Results.Ok(await service.GetFailureTrendsAsync(principal.ToUserContext(), ct)));

app.Run();

static async Task SeedDatabaseWithRetryAsync(IServiceProvider services)
{
    const int attempts = 10;
    for (var attempt = 1; attempt <= attempts; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var seed = scope.ServiceProvider.GetRequiredService<SeedData>();
            await seed.SeedAsync(CancellationToken.None);
            return;
        }
        catch when (attempt < attempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
        }
    }
}

internal static class ClaimsPrincipalExtensions
{
    public static UserContext ToUserContext(this ClaimsPrincipal principal)
    {
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.Parse<UserRole>(principal.FindFirstValue(ClaimTypes.Role)!);
        var companyIdValue = principal.FindFirstValue("companyId");
        var companyId = string.IsNullOrWhiteSpace(companyIdValue) ? (Guid?)null : Guid.Parse(companyIdValue);
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        return new UserContext(userId, role, companyId, email);
    }
}

internal sealed class JwtOptions
{
    public string Issuer { get; set; } = "PredictiveMaintenance";
    public string Audience { get; set; } = "PredictiveMaintenanceDashboard";
    public string SigningKey { get; set; } = "local-showcase-signing-key-change-before-production-32";
    public int ExpiresHours { get; set; } = 8;
}

internal sealed class AlertEvaluationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<AlertEvaluationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var created = await alertService.EvaluateAsync(stoppingToken);
                logger.LogInformation("Predictive alert evaluation completed. Created {CreatedCount} alerts.", created);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Predictive alert evaluation failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
