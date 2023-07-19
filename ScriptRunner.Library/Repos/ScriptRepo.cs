using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Settings;
using System.IO.Enumeration;

namespace ScriptRunner.Library.Repos
{
    public class ScriptRepo : IScriptRepo
    {
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<IScriptRepo> _logger;

        public ScriptRepo(IOptions<RepoSettings> options, ILogger<IScriptRepo> logger) : this(options.Value, logger)
        {
        }

        public ScriptRepo(RepoSettings repoSettings, ILogger<IScriptRepo> logger)
        {
            _repoSettings = repoSettings;
            _logger = logger;

            if (!Directory.Exists(_repoSettings.ScriptFolder))
                Directory.CreateDirectory(_repoSettings.ScriptFolder);
        }

        public async Task<IEnumerable<Package>> GetScriptsAsync()
        {
            var parsedScripts = new List<Package>();

            var extensions = new string[] { "*.sql", "*.ps1" };
            var files = Directory.EnumerateFiles(_repoSettings.ScriptFolder, "*.*", SearchOption.AllDirectories)
                                 .Where(filename => extensions.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, filename)));

            foreach (var file in files)
            {
                try
                {
                    var configFile = Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.config");
                    var config = await File.ReadAllTextAsync(configFile);
                    
                    Package sqlPackage = await LoadPackage(file, config);
                    parsedScripts.Add(sqlPackage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading {file}");
                }
            }

            _logger?.LogInformation($"{parsedScripts.Count()} Local script packages found");
            return parsedScripts;
        }

        private static async Task<Package> LoadPackage(string filename, string config)
        {
            return FileSystemName.MatchesSimpleExpression("*.sql", filename) ? await LoadSqlPackageAsync(filename, config)
                                                                             : await LoadPowershellPackageAsync(filename, config);
        }

        private static async Task<Package> LoadPowershellPackageAsync(string filename, string config)
        {
            var package = JsonConvert.DeserializeObject<Package>(config);
            var text = await File.ReadAllTextAsync(filename);

            package.ImportedDate ??= new FileInfo(filename).CreationTime;
            package.Scripts = new PowershellScript[] { new PowershellScript { Filename = filename, Script = text } };
            return package;
        }

        private static async Task<Package> LoadSqlPackageAsync(string filename, string config)
        {
            var sqlPackage = JsonConvert.DeserializeObject<SqlPackage>(config);
            var text = await File.ReadAllTextAsync(filename);

            sqlPackage.ImportedDate ??= new FileInfo(filename).CreationTime;
            sqlPackage.Scripts = new SimpleScript[] { new SqlScript { Filename = filename, Script = text, ConnectionString = sqlPackage.ConnectionString } };
            return sqlPackage;
        }
    }
}
