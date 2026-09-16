using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjectionManager
{
    public static IServiceCollection AddDiApplication(this IServiceCollection services)
    {
        //services.AddScoped<IUnitOfWork, UnitOfWork>();



        return services;
    }
}
