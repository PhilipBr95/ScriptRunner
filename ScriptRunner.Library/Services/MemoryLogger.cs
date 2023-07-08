using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ScriptRunner.Library.Services
{
    public class MemoryLogger : ILogger
    {
        private string _categoryName;
        private readonly System.Collections.Concurrent.ConcurrentQueue<MemoryLog> _messages = new();

        public IReadOnlyList<MemoryLog> Logs => new ReadOnlyCollection<MemoryLog>(_messages.ToArray());

        public MemoryLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            if (exception != null)
                message += Environment.NewLine + exception.ToString();

            _messages.Enqueue(new MemoryLog { LogLevel = logLevel, CategoryName = _categoryName, Message = message, CreatedDate = DateTime.Now });
        }
    }
}