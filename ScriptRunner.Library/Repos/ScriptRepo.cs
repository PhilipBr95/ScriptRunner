using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Packages;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Settings;
using System.IO.Enumeration;
using static System.Net.Mime.MediaTypeNames;

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

            var files = Directory.EnumerateFiles(_repoSettings.ScriptFolder, "config.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                _logger?.LogInformation($"Found Script folder {Path.GetDirectoryName(file)}");

                try
                {                                        
                    Package package = await LoadPackage(file);
                    parsedScripts.Add(package);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error loading {file}");
                }
            }

            files = Directory.EnumerateFiles(_repoSettings.ScriptFolder, "*.srunner", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                _logger?.LogInformation($"Found ScriptRunner file {file}");

                try
                {
                    var json = File.ReadAllText(file);
                    Package package = JsonConvert.DeserializeObject<Package>(json);
                    package.Filename = file;

                    parsedScripts.Add(package);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error loading {file}");
                }
            }

            _logger?.LogInformation($"{parsedScripts.Count()} Local script packages found");
            return parsedScripts;
        }

        private static async Task<Package> LoadPackage(string configFile)
        {
            var config = await File.ReadAllTextAsync(configFile);

            //TODO - This is a bit of a hack, but we need to deserialize to SqlPackage first
            var package = JsonConvert.DeserializeObject<SqlPackage>(config);

            package.ImportedDate ??= new FileInfo(configFile).CreationTime;
            package.Filename = configFile;

            //TODO - A bit of a hack, but check the ConnectionString is good
            foreach(var script in package.Scripts)
            {
                if (script.ScriptType == nameof(SqlScript))
                    (script as SqlScript)!.ConnectionString ??= package.ConnectionString;
            }

            var extensions = new string[] { "*.sql", "*.ps1" };
            var scriptFiles = Directory.EnumerateFiles(Path.GetDirectoryName(configFile), "*.*", SearchOption.AllDirectories)
                                        .Where(filename => extensions.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, filename)));

            var scripts = new List<SimpleScript>();

            //Add all the scripts
            foreach (var scriptFile in scriptFiles)
            {
                var scriptText = await File.ReadAllTextAsync(scriptFile);

                if (FileSystemName.MatchesSimpleExpression("*.sql", scriptFile))
                {
                    var connectString = package.ConnectionString;

                    //Do we need to override the default
                    if (Path.GetDirectoryName(configFile) != Path.GetDirectoryName(scriptFile))
                        connectString = ConnectionString.GetConnectionFromFilePath(scriptFile) ?? connectString;

                    scripts.Add(new SqlScript { Filename = scriptFile, Script = scriptText, ConnectionString = connectString });
                }
                else
                {
                    scripts.Add(new PowershellScript { Filename = scriptFile, Script = scriptText });
                }
            }

            if (scripts != null && package.Scripts != null)
            {
                scripts.AddRange(package.Scripts);
                package.Scripts = scripts;
            }
            else if (scripts != null)
            {
                package.Scripts = scripts;
            }

            return package;
        }

        public async Task<string> ImportPackageAsync(Package package)
        {
            try
            {
                package.ImportedDate = DateTime.Now;

                var json = JsonConvert.SerializeObject(package, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
                var category = package.Category ?? "All";
                var folder = Path.Combine(_repoSettings.ScriptFolder, category);
                var filename = Path.Combine(folder, $"{package.System}_{package.Id}_{package.Version}.srunner");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (File.Exists(filename))
                    throw new Exception($"The Package already exists - {filename}");

                await File.WriteAllTextAsync(filename, json);
                return filename;
            }
            catch(Exception ex) {
                _logger?.LogError(ex, $"Error Importing {package.UniqueId}");
                throw;
            }
        }

    }
}
