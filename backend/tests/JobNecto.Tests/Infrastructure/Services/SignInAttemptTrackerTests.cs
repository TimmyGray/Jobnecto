using FluentAssertions;
using JobNecto.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace JobNecto.Tests.Infrastructure.Services;

public class SignInAttemptTrackerTests
{
    private static SignInAttemptTracker CreateTracker(int maxAttempts = 5, string windowMinutes = "15")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:SignIn:MaxAttempts"] = maxAttempts.ToString(),
                ["RateLimit:SignIn:WindowMinutes"] = windowMinutes
            })
            .Build();

        return new SignInAttemptTracker(new MemoryCache(new MemoryCacheOptions()), configuration);
    }

    [Fact]
    public void MissingConfiguration_FallsBackToDefaultFiveAttemptThreshold()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var tracker = new SignInAttemptTracker(new MemoryCache(new MemoryCacheOptions()), emptyConfiguration);

        for (var i = 0; i < 4; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }
        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeFalse();

        tracker.RecordFailure("bob", "127.0.0.1");
        tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfter).Should().BeTrue();
        // Default window is 15 minutes, so retryAfter should be close to (but not exceed) that.
        retryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(15));
        retryAfter.Should().BeGreaterThan(TimeSpan.FromMinutes(14));
    }

    [Fact]
    public void IsLockedOut_NoPriorFailures_ReturnsFalse()
    {
        var tracker = CreateTracker();

        var lockedOut = tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfter);

        lockedOut.Should().BeFalse();
        retryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ThresholdBoundary_FifthFailure_IsLockedOut_SixthFailure_StaysLockedOut()
    {
        var tracker = CreateTracker(maxAttempts: 5);

        for (var i = 0; i < 5; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfterAtFive).Should().BeTrue();
        retryAfterAtFive.Should().BeGreaterThan(TimeSpan.Zero);

        tracker.RecordFailure("bob", "127.0.0.1");

        tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfterAtSix).Should().BeTrue();
        retryAfterAtSix.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void FourthFailure_NotYetLockedOut()
    {
        var tracker = CreateTracker(maxAttempts: 5);

        for (var i = 0; i < 4; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeFalse();
    }

    [Fact]
    public void Reset_OnSuccess_ClearsFailureCount()
    {
        var tracker = CreateTracker(maxAttempts: 5);

        for (var i = 0; i < 4; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.Reset("bob", "127.0.0.1");

        // 4 failures again after reset must not re-trigger lockout (AC 9: only 4 total post-reset).
        for (var i = 0; i < 4; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WindowExpiry_FailuresOutsideWindow_DoNotCountTowardLockout()
    {
        var tracker = CreateTracker(maxAttempts: 5, windowMinutes: "0.02"); // ~1.2s window

        for (var i = 0; i < 5; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeTrue();

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfter).Should().BeFalse();
        retryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CaseNormalizedKey_BobAndLowercaseBobAndUppercaseBob_ShareOneBucket()
    {
        var tracker = CreateTracker(maxAttempts: 5);

        tracker.RecordFailure("Bob", "127.0.0.1");
        tracker.RecordFailure("bob", "127.0.0.1");
        tracker.RecordFailure("BOB", "127.0.0.1");
        tracker.RecordFailure("Bob", "127.0.0.1");
        tracker.RecordFailure("bob", "127.0.0.1");

        tracker.IsLockedOut("BOB", "127.0.0.1", out _).Should().BeTrue();
        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeTrue();
    }

    [Fact]
    public void DifferentIps_ForSameIdentifier_TrackedIndependently()
    {
        var tracker = CreateTracker(maxAttempts: 5);

        for (var i = 0; i < 5; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        tracker.IsLockedOut("bob", "127.0.0.1", out _).Should().BeTrue();
        tracker.IsLockedOut("bob", "10.0.0.1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task FixedWindow_SubsequentFailureAfterLockout_DoesNotExtendTheWindow()
    {
        // Window is anchored to the first failure and must not slide on later failures.
        var tracker = CreateTracker(maxAttempts: 3, windowMinutes: "0.05"); // 3s window

        for (var i = 0; i < 3; i++)
        {
            tracker.RecordFailure("bob", "127.0.0.1");
        }

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // A failure recorded well after the window started, while still locked out.
        tracker.RecordFailure("bob", "127.0.0.1");

        tracker.IsLockedOut("bob", "127.0.0.1", out var retryAfter).Should().BeTrue();
        // If the window had reset to a fresh 3s, retryAfter would be close to 3s here.
        // Anchored to the first failure, only ~1.5s of the original 3s window remains.
        retryAfter.Should().BeLessThan(TimeSpan.FromSeconds(2.2));
    }
}
