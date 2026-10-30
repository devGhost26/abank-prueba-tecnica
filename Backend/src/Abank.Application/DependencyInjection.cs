using Abank.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Abank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
        });

        return services;
    }
}
