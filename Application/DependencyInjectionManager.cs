using Application.Interfaces.Services;
using Application.Interfaces.Strategies;
using Application.Services.Inquiry;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjectionManager
{
    public static IServiceCollection AddDiApplication(this IServiceCollection services)
    {

        services.AddScoped<IInquiryProviderService, InquiryProviderService>();


        ///use-case services 
        services.AddScoped<TrafficFineInquiry>();



        return services;
    }
}
