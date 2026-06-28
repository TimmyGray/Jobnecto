using FluentAssertions;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;

// Intentionally in the global namespace (matching PaginationTests): a
// `JobNecto.Tests.Domain` namespace would shadow `JobNecto.Domain` for tests
// that reference `Domain.Enums.*` relatively.
public class LanguageProficiencyTests
{
    [Fact]
    public void Constructor_SetsLanguageAndLevel()
    {
        var proficiency = new LanguageProficiency(Language.English, LanguageLevel.Advanced);

        proficiency.Language.Should().Be(Language.English);
        proficiency.Level.Should().Be(LanguageLevel.Advanced);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var a = new LanguageProficiency(Language.German, LanguageLevel.Intermediate);
        var b = new LanguageProficiency(Language.German, LanguageLevel.Intermediate);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentValues_AreNotEqual()
    {
        var a = new LanguageProficiency(Language.French, LanguageLevel.Beginner);
        var b = new LanguageProficiency(Language.French, LanguageLevel.Native);

        a.Should().NotBe(b);
    }
}
