using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace ScriptRunner.Library.Services
{
    public class MemoryLoggerProvider : IMemoryLoggerProvider, ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, MemoryLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new MemoryLogger(name));

        public void Dispose()
        {
            _loggers.Clear();
        }

        public IReadOnlyList<MemoryLog> MemoryLogs
        {
            get
            {
                var logs = _loggers.Values.SelectMany(s => s.Logs)
                                          .Where(w => w.CategoryName.StartsWith("Microsoft") == false)
                                          .OrderByDescending(o => o.CreatedDate)
                                          .ToArray();

                return new ReadOnlyCollection<MemoryLog>(logs);
            }
        }
    }
}
