using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Infrastructure.DataIo.Services;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security.Contexts;
using Infrastructure.Security.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjectionManager
{
    public static IServiceCollection AddDiInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDbFactoryContext, DbFactoryContext>();
        services.AddScoped<IEventManagerContext, EventManagerContext>();
        services.AddTransient<IFileStorageService, FileStorageService>();
        services.AddTransient<IFileReaderService, FileReaderService>();
        services.AddTransient<IExceptionLogService, ExceptionLogService>();


        //repositories
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }


}
