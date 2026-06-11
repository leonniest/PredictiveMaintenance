using Microsoft.Extensions.DependencyInjection;
using PredictiveMaintenance.Application.Services;

namespace PredictiveMaintenance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ISupportService, SupportService>();
        services.AddScoped<IAssistantService, AssistantService>();
        return services;
    }
}
