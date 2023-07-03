using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public class LoggerTransactionService : ITransactionService
    {
        private readonly ILogger<ITransactionService> _logger;

        public LoggerTransactionService(ILogger<ITransactionService> logger)
        {
            _logger = logger;
        }

        public async Task LogActivityAsync<T>(Activity<T> activity)
        {
            _logger?.LogInformation($"Fake Transaction Logger {activity.System}");

            await Task.CompletedTask;
        }
    }
}
