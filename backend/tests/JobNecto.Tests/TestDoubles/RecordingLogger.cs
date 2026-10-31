using Microsoft.Extensions.Logging;

namespace JobNecto.Tests.TestDoubles;

/// <summary>
/// <see cref="ILogger{TCategoryName}"/> that keeps every entry in memory so tests can assert on
/// level, rendered message and exception.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<Entry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new Entry(logLevel, formatter(state, exception), exception));
    }

    public sealed record Entry(LogLevel Level, string Message, Exception? Exception);
}
