using ScriptRunner.Library.Models;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Fakes
{
    public class FakeTransactionService : ITransactionService
    {
        private readonly ILogger<ITransactionService> _logger;

        public FakeTransactionService(ILogger<ITransactionService> logger)
        {
            _logger = logger;
        }

        public void LogActivity<T>(Activity<T> activity)
        {
            _logger.LogInformation($"Logging Fake transaction for {activity.System}");
        }
    }
}