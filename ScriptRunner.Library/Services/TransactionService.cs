using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<ITransactionService> _logger;

        public TransactionService(ILogger<ITransactionService> logger)
        {
            _logger = logger;
        }

        public async Task LogActivityAsync<T>(Activity<T> activity)
        {
            _logger?.LogInformation($"Activity Executed {JsonConvert.SerializeObject(activity)}");

            await Task.CompletedTask;
        }
    }
}