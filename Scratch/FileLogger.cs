#if DEBUG
using Microsoft.Extensions.Logging;

namespace Scratch;

/// <summary>
/// Per-category file sink for <see cref="ILogger"/>. Synchronized writes, append:false on
/// open to match the old NLog config's <c>deleteOldFileOnStartup="true"</c>, Debug threshold.
/// Compiled only in Debug builds — Release skips the file IO and the class altogether.
/// </summary>
internal sealed class FileLogger : ILogger, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _gate = new();

    public FileLogger(string path)
    {
        _writer = new StreamWriter(path, append: false) { AutoFlush = true };
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        lock (_gate)
        {
            _writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            _writer.Write(" [");
            _writer.Write(logLevel.ToString().ToUpperInvariant());
            _writer.Write("] ");
            _writer.WriteLine(message);
            if (exception is not null)
            {
                _writer.WriteLine(exception);
            }
        }
    }

    public void Dispose() => _writer.Dispose();
}
#endif
