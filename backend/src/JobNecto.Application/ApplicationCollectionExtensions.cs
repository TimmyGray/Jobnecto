using FluentValidation;
using JobNecto.Application.Behaviors;
using JobNecto.Application.JobSources;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobNecto.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class ApplicationCollectionExtensions
{
    /// <summary>
    /// Registers MediatR (requests and notification handlers), FluentValidation validators, the validation
    /// pipeline behavior, and the vacancy ingestion service. The <see cref="IJobSource"/> it ingests from is
    /// registered by the job-source infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateUserCommandValidator).Assembly);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IVacancyIngestionService, VacancyIngestionService>();

        return services;
    }
}
