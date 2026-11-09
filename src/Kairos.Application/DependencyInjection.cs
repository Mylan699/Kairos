using FluentValidation;
using Kairos.Application.Trades;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<TradeRequestValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TradeService>();

        return services;
    }
}
