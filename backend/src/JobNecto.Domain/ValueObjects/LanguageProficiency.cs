using JobNecto.Domain.Enums;

namespace JobNecto.Domain.ValueObjects;

public record LanguageProficiency
{
    public Language Language { get; init; }
    public LanguageLevel Level { get; init; }

    public LanguageProficiency(Language language, LanguageLevel level)
    {
        Language = language;
        Level = level;
    }
}
