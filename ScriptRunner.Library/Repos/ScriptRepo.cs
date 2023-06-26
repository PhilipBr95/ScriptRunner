using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Settings;

namespace ScriptRunner.Library.Repos
{
    public class ScriptRepo : IScriptRepo
    {
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<INugetRepo> _logger;

        public ScriptRepo(IOptions<RepoSettings> options, ILogger<INugetRepo> logger) : this(options.Value, logger)
        {
        }

        public ScriptRepo(RepoSettings repoSettings, ILogger<INugetRepo> logger)
        {
            _repoSettings = repoSettings;
            _logger = logger;
        }

        public async Task<IEnumerable<Package>> GetScriptsAsync()
        {
            var parsedScripts = new List<Package>();
            var files = Directory.EnumerateFiles(_repoSettings.ScriptFolder, "*.sql");

            foreach (var file in files)
            {
                try
                {
                    var config = await File.ReadAllTextAsync(Path.Combine(_repoSettings.ScriptFolder, $"{Path.GetFileNameWithoutExtension(file)}.config"));
                    var sqlPackage = JsonConvert.DeserializeObject<SqlPackage>(config);
                    var text = await File.ReadAllTextAsync(file);

                    sqlPackage.CreationTime ??= new FileInfo(file).CreationTime; 
                    sqlPackage.Scripts = new BaseScript[] { new SqlScript { Filename = file, Script = text, ConnectionString = sqlPackage.ConnectionString } };

                    parsedScripts.Add(sqlPackage);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"Error loading {file}");
                }
            }

            return parsedScripts;
        }
    }
}
