using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

/// <summary>Captures every log entry the test application writes so tests can assert on category, event id, level and rendered message.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public IEnumerable<LogEntry> Smtp2Go => Entries.Where(entry => entry.Category == Smtp2GoWebhookEventIds.CategoryName);

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturingLogger(categoryName, Entries);
    }

    public void Dispose()
    {
    }

    public sealed record LogEntry(string Category, LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(category, logLevel, eventId, formatter(state, exception), exception));
        }
    }
}
