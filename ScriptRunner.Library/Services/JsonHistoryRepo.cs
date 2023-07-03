using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Models;

namespace ScriptRunner.UI.Services
{
    public class JsonHistoryRepo : IHistoryRepo
    {
        private readonly HistorySettings _historySettings;
        private readonly ILogger<IHistoryRepo> _logger;
        private readonly SemaphoreSlim _repoLock = new SemaphoreSlim(1);

        public JsonHistoryRepo(IOptions<HistorySettings> options, ILogger<IHistoryRepo> logger)
        {
            _historySettings = options.Value;
            _logger = logger;
        }

        public async Task SaveActiviesAsync<T>(IList<Activity<T>> activities)
        {
            try
            {
                await _repoLock.WaitAsync();

                var json = JsonConvert.SerializeObject(activities, Formatting.Indented);

                //Back it up
                if (File.Exists(_historySettings.Filename))
                    File.Move(_historySettings.Filename, $"{_historySettings.BackupFilename}", true);

                await File.WriteAllTextAsync(_historySettings.Filename, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error saving repo");
                throw;
            }
            finally
            {
                _repoLock.Release();
            }
        }

        public async Task<IList<Activity<T>>> LoadActivitiesAsync<T>()
        {
            try
            {
                await _repoLock.WaitAsync();

                if (Directory.Exists(_historySettings.Filename) == false)
                    Directory.CreateDirectory(_historySettings.Folder);

                if (File.Exists(_historySettings.Filename) == false)
                    return new List<Activity<T>>();

                var json = await File.ReadAllTextAsync(_historySettings.Filename);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<Activity<T>>();

                return JsonConvert.DeserializeObject<IList<Activity<T>>>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error loading repo");
                throw;
            }
            finally
            {
                _repoLock.Release();
            }
        }
    }
}