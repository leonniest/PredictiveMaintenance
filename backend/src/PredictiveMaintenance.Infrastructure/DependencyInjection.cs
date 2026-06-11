using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Infrastructure.Assistant;
using PredictiveMaintenance.Infrastructure.Notifications;
using PredictiveMaintenance.Infrastructure.Persistence;
using PredictiveMaintenance.Infrastructure.Security;

namespace PredictiveMaintenance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PredictiveMaintenanceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PredictiveMaintenance")));

        services.AddScoped<IPredictiveMaintenanceDbContext>(provider => provider.GetRequiredService<PredictiveMaintenanceDbContext>());
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.Configure<DeepSeekOptions>(configuration.GetSection("DeepSeek"));
        services.AddSingleton<HttpClient>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IDeepSeekClient, DeepSeekClient>();
        services.AddKeyedScoped<IAlertNotifier, SmtpAlertNotifier>("smtp");
        services.AddKeyedScoped<IAlertNotifier, RabbitMqAlertNotifier>("rabbitmq");
        services.AddScoped<IAlertNotifier, ConfiguredAlertNotifier>();
        services.AddScoped<SeedData>();

        return services;
    }
}
