using JobNecto.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace JobNecto.Infrastructure.Services;

/// <summary>
/// <see cref="IMemoryCache"/>-backed implementation of <see cref="ISignInAttemptTracker"/>.
/// Uses a fixed window anchored to the first failure: the window's absolute expiration is set once
/// and does not slide on subsequent failures, so it evicts on its own after
/// <c>RateLimit:SignIn:WindowMinutes</c> regardless of further activity.
/// </summary>
public sealed class SignInAttemptTracker : ISignInAttemptTracker
{
    private const int DefaultMaxAttempts = 5;
    private const double DefaultWindowMinutes = 15;
    private const string CacheKeyPrefix = "sign-in-attempts";

    private readonly IMemoryCache _cache;
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;

    public SignInAttemptTracker(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _maxAttempts = int.TryParse(configuration["RateLimit:SignIn:MaxAttempts"], out var maxAttempts)
            ? maxAttempts
            : DefaultMaxAttempts;
        _window = TimeSpan.FromMinutes(
            double.TryParse(configuration["RateLimit:SignIn:WindowMinutes"], out var windowMinutes)
                ? windowMinutes
                : DefaultWindowMinutes);
    }

    /// <inheritdoc />
    public bool IsLockedOut(string identifier, string ip, out TimeSpan retryAfter)
    {
        var state = _cache.Get<AttemptState>(BuildKey(identifier, ip));

        if (state is null || state.FailureCount < _maxAttempts)
        {
            retryAfter = TimeSpan.Zero;
            return false;
        }

        var remaining = state.WindowExpiresAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            retryAfter = TimeSpan.Zero;
            return false;
        }

        retryAfter = remaining;
        return true;
    }

    /// <inheritdoc />
    public void RecordFailure(string identifier, string ip)
    {
        var key = BuildKey(identifier, ip);
        var existing = _cache.Get<AttemptState>(key);

        var windowExpiresAt = existing?.WindowExpiresAt ?? DateTimeOffset.UtcNow.Add(_window);
        var failureCount = (existing?.FailureCount ?? 0) + 1;

        _cache.Set(key, new AttemptState(failureCount, windowExpiresAt), windowExpiresAt);
    }

    /// <inheritdoc />
    public void Reset(string identifier, string ip)
    {
        _cache.Remove(BuildKey(identifier, ip));
    }

    private static string BuildKey(string identifier, string ip)
    {
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
        return $"{CacheKeyPrefix}:{normalizedIdentifier}:{ip}";
    }

    private sealed record AttemptState(int FailureCount, DateTimeOffset WindowExpiresAt);
}
