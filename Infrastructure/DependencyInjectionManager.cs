using Domain.Interfaces.Contexts;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Security.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjectionManager
{
    public static IServiceCollection AddDiInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDbFactoryContext, DbFactoryContext>();
        services.AddScoped<IEventManagerContext, EventManagerContext>();


        return services;
    }
}
