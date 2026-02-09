using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Response;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Infrastructure.Detection;
using AccessWatchLite.Infrastructure.Detection.Signals;
using AccessWatchLite.Infrastructure.Response.Actions;
using AccessWatchLite.Infrastructure.Services;
using AccessWatchLite.Infrastructure.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace AccessWatchLite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        
        // Repositories
        services.AddScoped<ISimEventRepository, SimEventRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAccessEventRepository, AccessEventRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<INameMappingRepository, NameMappingRepository>();
        services.AddScoped<ISimMetadataRepository, SimMetadataRepository>();
        services.AddScoped<IUserBehaviorProfileRepository, UserBehaviorProfileRepository>();
        services.AddScoped<IResponseActionRepository, ResponseActionRepository>();
        
        // Services
        services.AddSingleton<IAnalysisProgressService, AnalysisProgressService>(); // Singleton para progreso compartido
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<INamePseudonymizationService, NamePseudonymizationService>();
        services.AddScoped<ISimAnalysisService, SimAnalysisService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IResponseService, ResponseService>();
        
        // Detection Configuration (Singleton - misma config para todos)
        services.AddSingleton<DetectionConfig>();
        
        // Detection Signals (6 señales primarias)
        services.AddScoped<ISignal, UnusualLocationSignal>();
        services.AddScoped<ISignal, IpChangeSignal>();
        services.AddScoped<ISignal, UnknownDeviceSignal>();
        services.AddScoped<ISignal, AtypicalTimeSignal>();
        services.AddScoped<ISignal, WeakAuthSignal>();
        services.AddScoped<ISignal, FailedAttemptsSignal>();
        
        // Detection Engine
        services.AddScoped<IRiskDetectionEngine, RiskDetectionEngine>();

        // Response Actions (5 acciones de respuesta)
        services.AddScoped<IResponseAction, RevokeSessionAction>();
        services.AddScoped<IResponseAction, BlockUserAction>();
        services.AddScoped<IResponseAction, RequireMfaAction>();
        services.AddScoped<IResponseAction, NotifyEmailAction>();
        services.AddScoped<IResponseAction, LogIncidentAction>();

        return services;
    }
}
