using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHistoryRepo _historyRepo;
        private readonly ILogger<IHistoryService> _logger;

        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);

        public HistoryService(IMemoryCache memoryCache, IHistoryRepo historyRepo, ILogger<IHistoryService> logger)
        {
            _memoryCache = memoryCache;
            _historyRepo = historyRepo;
            _logger = logger;
        }

        public async Task LogActivityAsync<T>(Activity<T> activity)
        {
            try
            {
                await _cacheLock.WaitAsync();
                
                var activities = await GetActivitiesAsync<T>();
                activities.Add(activity);

                //if(_memoryCache.TryGetValue(nameof(GetActivitiesAsync), out IList<Activity<T>> values))
                //    values.Add(activity);

                _logger.LogInformation($"Saving {activity.System} transaction");

                await _historyRepo.SaveActiviesAsync(activities);
            }
            finally
            {
                _cacheLock.Release();
            }            
        }

        public async Task<IList<Activity<T>>> GetActivitiesAsync<T>()
        {
            return await _memoryCache.GetOrCreateAsync(nameof(GetActivitiesAsync), async (cacheEntry) =>
            {
                return await _historyRepo.LoadActivitiesAsync<T>();
            });
        }
    }
}