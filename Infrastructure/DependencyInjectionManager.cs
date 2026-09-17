using Application.Interfaces.Strategies;
using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories.Audit;
using Domain.Interfaces.Repositories.Inquriy;
using Domain.Interfaces.Services;
using Infrastructure.DataIo.Services;
using Infrastructure.Integration.Contexts;
using Infrastructure.Integration.Factories.Inquriy;
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
        services.AddTransient<IHttpIntegrationContext, HttpIntegrationContext>();


        //repositories
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IInquiryProviderRepository, InquiryProviderRepository>();
        services.AddScoped<IInquiryVehicleViolationRepository, InquiryVehicleViolationRepository>();


        //factories
        services.AddScoped<IInquiryFactory, InquiryFactory>();
        services.AddScoped<Provider1VehicleViolation>();
        services.AddScoped<Provider2VehicleViolation>();
        services.AddScoped<Provider3VehicleViolation>();
        services.AddScoped<Provider4VehicleViolation>();
        services.AddScoped<Provider5VehicleViolation>();



        return services;
    }


}
