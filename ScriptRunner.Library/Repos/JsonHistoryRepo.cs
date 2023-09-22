﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Helpers;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Settings;
using ScriptRunner.UI.Services;
using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Repos
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

            if (!Directory.Exists(_historySettings.Folder))
                Directory.CreateDirectory(_historySettings.Folder);
        }

        public async Task SaveActivityAsync<T>(Activity<T> activitity)
        {
            try
            {
                var activities = await LoadActivitiesAsync<T>();

                await _repoLock.WaitAsync();
              
                activities.Add(activitity);

                BackupRepo();

                var json = JsonConvert.SerializeObject(activities, Formatting.Indented);
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

        private void BackupRepo()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_historySettings.Filename) == false)
                {
                    if (File.Exists(_historySettings.Filename))
                    {
                        if (string.IsNullOrWhiteSpace(_historySettings.DailyBackupFilename) == false)
                        {
                            //Create a daily backup
                            var regex = new Regex("^.*{([\\w]+)}");
                            var dailyBackup = regex.Replace(_historySettings.DailyBackupFilename, (s) => s.Value.Replace($"{{{s.Groups[1].Value}}}", DateTime.Now.ToString(s.Groups[1].Value)));

                            //Once a day backup
                            if (File.Exists(dailyBackup) == false)
                            {
                                //Back it up
                                File.Copy(_historySettings.Filename, dailyBackup, true);

                                //Get rid of really old backups
                                var backupToDelete = Directory.GetFiles(_historySettings.Folder, Path.GetExtension(_historySettings.Filename))
                                                              .Select(s => new FileInfo(s))
                                                              .Where(w => w.CreationTime.AddDays(_historySettings.MaxBackupAgeInDays) < DateTime.Now)
                                                              .ToList();

                                backupToDelete.ForEach(w => w.Delete());
                            }
                        }

                        //Back it up
                        if (File.Exists(_historySettings.Filename))
                            File.Move(_historySettings.Filename, $"{_historySettings.BackupFilename}", true);
                    }
                }
            }
            catch(Exception ex) 
            {
                _logger.LogError($"Error backing up history - {ex}");
                throw;
            }
        }

        public async Task<IList<Activity<T>>> LoadActivitiesAsync<T>()
        {
            try
            {
                await _repoLock.WaitAsync();
                
                if (File.Exists(_historySettings.Filename) == false)
                    return new List<Activity<T>>();

                var json = await File.ReadAllTextAsync(_historySettings.Filename);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<Activity<T>>();

                return JsonConvert.DeserializeObject<IList<Activity<T>>>(json, new JsonSerializerSettings { Error = (sender, errorArgs) => 
                {
                    _logger.LogError($"Unhandled Deserialision Error in {nameof(LoadActivitiesAsync)} - {errorArgs.ErrorContext.Error.Message}");
                    errorArgs.ErrorContext.Handled = true;
                }  
                })!;
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