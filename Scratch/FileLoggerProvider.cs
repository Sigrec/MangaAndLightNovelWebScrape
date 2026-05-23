#if DEBUG
using Microsoft.Extensions.Logging;

namespace Scratch;

/// <summary>
/// Writes per-category log files under <c>logsDir</c>, mirroring the per-logger-file pattern
/// the old NLog.config used (<c>Crunchyroll_Logs.log</c>, <c>BooksAMillion_Logs.log</c>, etc.).
/// Categories are typically full type names; the trailing segment is the file's short name.
/// Compiled only in Debug builds.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logsDir;
    private readonly Dictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public FileLoggerProvider(string logsDir)
    {
        _logsDir = logsDir;
        Directory.CreateDirectory(_logsDir);
    }

    public ILogger CreateLogger(string categoryName)
    {
        lock (_gate)
        {
            if (!_loggers.TryGetValue(categoryName, out FileLogger? logger))
            {
                int lastDot = categoryName.LastIndexOf('.');
                string shortName = lastDot >= 0 ? categoryName[(lastDot + 1)..] : categoryName;
                string path = Path.Combine(_logsDir, $"{shortName}_Logs.log");
                logger = new FileLogger(path);
                _loggers[categoryName] = logger;
            }
            return logger;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (FileLogger logger in _loggers.Values)
            {
                logger.Dispose();
            }
            _loggers.Clear();
        }
    }
}
#endif
