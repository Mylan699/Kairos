using FluentValidation;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Risk;
using Kairos.Application.Trades;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<TradeRequestValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PortfolioCache>();
        services.AddScoped<TradeService>();
        services.AddScoped<PortfolioService>();
        services.AddScoped<RiskService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<MarketService>();

        return services;
    }
}
