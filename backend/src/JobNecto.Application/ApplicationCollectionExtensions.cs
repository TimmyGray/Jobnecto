using FluentValidation;
using JobNecto.Application.Behaviors;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class ApplicationCollectionExtensions
{
    /// <summary>
    /// Registers MediatR, FluentValidation validators, and the validation pipeline behavior.
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

        return services;
    }
}
