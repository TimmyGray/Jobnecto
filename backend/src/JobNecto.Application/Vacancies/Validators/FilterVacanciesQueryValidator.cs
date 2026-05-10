using FluentValidation;

namespace JobNecto.Application.Vacancies.Validators;

/// <summary>
/// Validates <see cref="FilterVacanciesQuery"/> for cross-field salary constraints.
/// Discovered automatically by <c>AddValidatorsFromAssembly</c> and executed by the MediatR
/// validation pipeline behavior before the handler runs.
/// </summary>
public class FilterVacanciesQueryValidator : AbstractValidator<FilterVacanciesQuery>
{
    /// <summary>
    /// Initializes validation rules for the vacancy filter query.
    /// </summary>
    public FilterVacanciesQueryValidator()
    {
        RuleFor(x => x.SalaryMin)
            .LessThanOrEqualTo(x => x.SalaryMax)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue)
            .WithMessage("salaryMin must not be greater than salaryMax.");

        RuleFor(x => x.ExcludeKeywords)
            .Must(kw => kw == null || kw.Length <= 20)
            .WithMessage("At most 20 exclude keywords are allowed.");
    }
}
