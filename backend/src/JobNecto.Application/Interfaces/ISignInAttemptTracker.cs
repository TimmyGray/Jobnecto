namespace JobNecto.Application.Interfaces;

/// <summary>
/// Tracks failed sign-in attempts per (identifier, client IP) pair to enforce a rate limit.
/// Only failures count toward the limit; a successful sign-in resets the tracked window.
/// </summary>
public interface ISignInAttemptTracker
{
    /// <summary>
    /// Returns <c>true</c> when the (identifier, IP) pair has exceeded the configured failure
    /// threshold within the current window. <paramref name="retryAfter"/> is the remaining time
    /// until the window clears; it is <see cref="TimeSpan.Zero"/> when not locked out.
    /// </summary>
    bool IsLockedOut(string identifier, string ip, out TimeSpan retryAfter);

    /// <summary>
    /// Records a failed sign-in attempt for the (identifier, IP) pair.
    /// </summary>
    void RecordFailure(string identifier, string ip);

    /// <summary>
    /// Clears the tracked failure window for the (identifier, IP) pair, e.g. after a successful sign-in.
    /// </summary>
    void Reset(string identifier, string ip);
}
