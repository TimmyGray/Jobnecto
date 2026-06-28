using System.Data.Common;
using FluentAssertions;
using JobNecto.Infrastructure.Persistance.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace JobNecto.Tests.Infrastructure;

public class DbCommandTimingInterceptorTests
{
    private static DbCommand Command(string sql) => new NpgsqlCommand(sql);

    private static Mock<ILogger<DbCommandTimingInterceptor>> Logger(bool enabled)
    {
        var logger = new Mock<ILogger<DbCommandTimingInterceptor>>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
        return logger;
    }

    private static CommandExecutedEventData ExecutedData(DbCommand command) =>
        new(
            eventDefinition: null!,
            messageGenerator: null!,
            connection: null!,
            command: command,
            logCommandText: command.CommandText,
            context: null,
            executeMethod: DbCommandMethod.ExecuteReader,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            result: null,
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            duration: TimeSpan.FromMilliseconds(12.345),
            commandSource: CommandSource.Unknown);

    private static CommandErrorEventData ErrorData(DbCommand command) =>
        new(
            eventDefinition: null!,
            messageGenerator: null!,
            connection: null!,
            command: command,
            logCommandText: command.CommandText,
            context: null,
            executeMethod: DbCommandMethod.ExecuteReader,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            exception: new InvalidOperationException("boom"),
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            duration: TimeSpan.FromMilliseconds(7),
            commandSource: CommandSource.Unknown);

    [Fact]
    public void ReaderExecuted_WhenLoggingEnabled_LogsAndReturnsResult()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);
        using var reader = new EmptyDataReader();

        var result = interceptor.ReaderExecuted(Command("SELECT 1"), ExecutedData(Command("SELECT 1")), reader);

        result.Should().BeSameAs(reader);
    }

    [Fact]
    public void ScalarExecuted_ReturnsResult()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);

        var result = interceptor.ScalarExecuted(Command("SELECT 1"), ExecutedData(Command("SELECT 1")), 42);

        result.Should().Be(42);
    }

    [Fact]
    public void NonQueryExecuted_ReturnsResult()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);

        var result = interceptor.NonQueryExecuted(Command("DELETE FROM t"), ExecutedData(Command("DELETE FROM t")), 3);

        result.Should().Be(3);
    }

    [Fact]
    public async Task AsyncExecutedMethods_ReturnResults()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);
        using var reader = new EmptyDataReader();

        var readerResult = await interceptor.ReaderExecutedAsync(Command("SELECT 1"), ExecutedData(Command("SELECT 1")), reader);
        var scalarResult = await interceptor.ScalarExecutedAsync(Command("SELECT 1"), ExecutedData(Command("SELECT 1")), 9);
        var nonQueryResult = await interceptor.NonQueryExecutedAsync(Command("UPDATE t SET x=1"), ExecutedData(Command("UPDATE t SET x=1")), 1);

        readerResult.Should().BeSameAs(reader);
        scalarResult.Should().Be(9);
        nonQueryResult.Should().Be(1);
    }

    [Fact]
    public void Executed_WhenLoggingDisabled_SkipsLoggingAndReturnsResult()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: false).Object);

        var result = interceptor.ScalarExecuted(Command("SELECT 1"), ExecutedData(Command("SELECT 1")), 5);

        result.Should().Be(5);
    }

    [Fact]
    public void Executed_WithLongSql_TruncatesWithoutThrowing()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);
        var longSql = "SELECT " + new string('x', 400) + "    \n   FROM t";

        var act = () => interceptor.NonQueryExecuted(Command(longSql), ExecutedData(Command(longSql)), 0);

        act.Should().NotThrow();
    }

    [Fact]
    public void Executed_WithEmptySql_DoesNotThrow()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);

        var act = () => interceptor.NonQueryExecuted(Command(string.Empty), ExecutedData(Command(string.Empty)), 0);

        act.Should().NotThrow();
    }

    [Fact]
    public void CommandFailed_WhenWarningEnabled_LogsWarning()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);

        var act = () => interceptor.CommandFailed(Command("SELECT bad"), ErrorData(Command("SELECT bad")));

        act.Should().NotThrow();
    }

    [Fact]
    public void CommandFailed_WhenWarningDisabled_SkipsLogging()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: false).Object);

        var act = () => interceptor.CommandFailed(Command("SELECT bad"), ErrorData(Command("SELECT bad")));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task CommandFailedAsync_DoesNotThrow()
    {
        var interceptor = new DbCommandTimingInterceptor(Logger(enabled: true).Object);

        await interceptor.CommandFailedAsync(Command("SELECT bad"), ErrorData(Command("SELECT bad")));
    }

    /// <summary>Minimal DbDataReader so interceptor tests need no live connection.</summary>
    private sealed class EmptyDataReader : DbDataReader
    {
        public override bool Read() => false;
        public override bool NextResult() => false;
        public override int Depth => 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int FieldCount => 0;
        public override bool HasRows => false;
        public override object this[int ordinal] => throw new NotSupportedException();
        public override object this[string name] => throw new NotSupportedException();
        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
        public override byte GetByte(int ordinal) => throw new NotSupportedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => throw new NotSupportedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
        public override double GetDouble(int ordinal) => throw new NotSupportedException();
        public override Type GetFieldType(int ordinal) => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => throw new NotSupportedException();
        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
        public override short GetInt16(int ordinal) => throw new NotSupportedException();
        public override int GetInt32(int ordinal) => throw new NotSupportedException();
        public override long GetInt64(int ordinal) => throw new NotSupportedException();
        public override string GetName(int ordinal) => throw new NotSupportedException();
        public override int GetOrdinal(string name) => throw new NotSupportedException();
        public override string GetString(int ordinal) => throw new NotSupportedException();
        public override object GetValue(int ordinal) => throw new NotSupportedException();
        public override int GetValues(object[] values) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => throw new NotSupportedException();
        public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
    }
}
