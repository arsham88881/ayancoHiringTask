using Application.Interfaces.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Factories.Inquriy;

internal class InquiryFactory : IInquiryFactory
{

    private readonly IServiceProvider sp;
    private readonly Dictionary<string, Type> registerdProviders;

    public InquiryFactory(IServiceProvider serviceProvider)
    {
        sp = serviceProvider;
        registerdProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Provider1"] = typeof(Provider1VehicleViolation),
            ["Provider2"] = typeof(Provider2VehicleViolation),
            ["Provider3"] = typeof(Provider3VehicleViolation),
            ["Provider4"] = typeof(Provider4VehicleViolation),
            ["Provider5"] = typeof(Provider5VehicleViolation)
        };
    }

    public IInquiryVehicleViolation GetInstance(string provider)
    {
        var providerInstance = registerdProviders[provider];
        return (IInquiryVehicleViolation)sp.GetRequiredService(providerInstance);
    }
}
