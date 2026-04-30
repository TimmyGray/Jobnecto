using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JobNecto.Infrastructure.Persistance.Interceptors;

/// <summary>
/// Logs execution time for each EF Core database command.
/// Useful for measuring real DB round-trips and command latency on production-like databases.
/// </summary>
public sealed class DbCommandTimingInterceptor : DbCommandInterceptor
{
    private readonly ILogger<DbCommandTimingInterceptor> _logger;

    public DbCommandTimingInterceptor(ILogger<DbCommandTimingInterceptor> logger)
    {
        _logger = logger;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogCommandExecuted(command, eventData, "reader");
        return result;
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        LogCommandExecuted(command, eventData, "scalar");
        return result;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogCommandExecuted(command, eventData, "non-query");
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "reader");
        return ValueTask.FromResult(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "scalar");
        return ValueTask.FromResult(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "non-query");
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        if (!_logger.IsEnabled(LogLevel.Warning))
            return;

        _logger.LogWarning(
            eventData.Exception,
            "DB command failed after {DurationMs:0.###} ms | Sql={Sql}",
            eventData.Duration.TotalMilliseconds,
            CompactSql(command.CommandText)
        );
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        CommandFailed(command, eventData);
        return Task.CompletedTask;
    }

    private void LogCommandExecuted(DbCommand command, CommandExecutedEventData eventData, string kind)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.LogInformation(
            "DB {Kind} executed in {DurationMs:0.###} ms | Sql={Sql}",
            kind,
            eventData.Duration.TotalMilliseconds,
            CompactSql(command.CommandText)
        );
    }

    private static string CompactSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        var compact = Regex.Replace(sql, @"\s+", " ").Trim();
        const int maxLength = 300;
        return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
    }
}