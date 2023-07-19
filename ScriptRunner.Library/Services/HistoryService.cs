using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepo _historyRepo;
        private readonly ILogger<IHistoryService> _logger;

        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);

        public HistoryService(IHistoryRepo historyRepo, ILogger<IHistoryService> logger)
        {            
            _historyRepo = historyRepo;
            _logger = logger;
        }

        public async Task LogActivityAsync<T>(Activity<T> activity)
        {
            await _historyRepo.SaveActivityAsync(activity);
        }

        public async Task<IList<Activity<T>>> GetActivitiesAsync<T>()
        {
            return await _historyRepo.LoadActivitiesAsync<T>();            
        }
    }
}