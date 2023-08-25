using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using System.IO.Enumeration;
using System.Text;

namespace ScriptRunner.Library.Repos
{
    public class NugetRepo : INugetRepo
    {
        private readonly ITransactionService _transactionService;
        private readonly IHistoryService _historyService;
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<INugetRepo> _logger;

        public NugetRepo(ITransactionService transactionService, IHistoryService historyService, IOptions<RepoSettings> options, ILogger<INugetRepo> logger) : this(transactionService, historyService, options.Value, logger)
        {            
        }

        public NugetRepo(ITransactionService transactionService, IHistoryService historyService, RepoSettings repoSettings, ILogger<INugetRepo> logger)
        {
            _transactionService = transactionService;
            _historyService = historyService;
            _repoSettings = repoSettings;
            _logger = logger;

            if (!Directory.Exists(_repoSettings.NugetFolder))
                Directory.CreateDirectory(_repoSettings.NugetFolder);
        }

        public async Task<IEnumerable<Package>> GetScriptsAsync()
        {
            var parsedScripts = new List<Package>();
            var files = Directory.EnumerateFiles(_repoSettings.NugetFolder, "*.nupkg");

            foreach (var file in files)
            {
                _logger?.LogInformation($"Found Nuget Package {file}");
                parsedScripts.Add(GenerateScript(file));
            }

            _logger?.LogInformation($"{parsedScripts.Count()} Local nuget packages found");
            return parsedScripts;
        }

        public async Task ImportScriptsAsync(string user, string[] packageIds)
        {
            try
            {
                var newPackages = await ListScriptsAsync(packageIds);
                
                var packageSource = new PackageSource(_repoSettings.GitRepo);
                var repository = Repository.Factory.GetCoreV3(packageSource);

                SourceCacheContext cache = new SourceCacheContext();
                PackageSearchResource packageSearchResource = await repository.GetResourceAsync<PackageSearchResource>();
                FindPackageByIdResource findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>();

                var logger = NuGet.Common.NullLogger.Instance;
                var cancellationToken = CancellationToken.None;

                foreach (var result in newPackages)
                {
                    var filename = Path.Combine(_repoSettings.NugetFolder, $"{result.Id}.{result.Version}.nupkg");

                    if (!File.Exists(filename))
                    {
                        //Wipe the old versions
                        Directory.GetFiles(_repoSettings.NugetFolder, $"{result.Id}.*.nupkg")
                                    .ToList()
                                    .ForEach(fe => File.Delete(fe));

                        using (var packageStream = File.OpenWrite(filename))
                        {
                            var success = await findPackageByIdResource.CopyNupkgToStreamAsync(
                                result.Id, // package id                                
                                NuGetVersion.Parse(result.Version),
                                packageStream,
                                cache,
                                logger,
                                cancellationToken);

                            await packageStream.FlushAsync();
                            packageStream.Close();

                            if (success)
                            {
                                _logger.LogInformation($"Downloaded {result.Id} version {result.Version} - {success}");

                                await LogActivity(new Activity<Package> { ActionedBy = user, System = "ScriptRunner", Success = success, Description = $"Package imported - {result.Id} version {result.Version}" });
                            }
                            else
                                _logger.LogError($"Failed to Download {result.Id} version {result.Version}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error retrieving packages from {_repoSettings.GitRepo}");
                throw;
            }
        }

        private async Task LogActivity(Activity<Package> activity)
        {
            await _historyService.LogActivityAsync(activity);
            await _transactionService.LogActivityAsync(activity);
        }

        public async Task<IEnumerable<Package>> ListScriptsAsync(string[]? packageIds = null)
        {
            var packages = new List<Package>();

            if (packageIds == null || packageIds.Length == 0)
                packageIds = new string[] { "" };   //List everything

            try
            {
                var packageSource = new PackageSource(_repoSettings.GitRepo);
                var repository = Repository.Factory.GetCoreV3(packageSource);

                SourceCacheContext cache = new SourceCacheContext();
                PackageSearchResource packageSearchResource = await repository.GetResourceAsync<PackageSearchResource>();
                FindPackageByIdResource findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>();
                SearchFilter searchFilter = new SearchFilter(includePrerelease: true);

                var logger = NuGet.Common.NullLogger.Instance;
                var cancellationToken = CancellationToken.None;

                foreach (var packageId in packageIds)
                {
                    if(string.IsNullOrWhiteSpace(packageId) == false)
                        _logger.LogInformation($"Finding Remote package {packageId}");

                    var results = (await packageSearchResource.SearchAsync(
                        packageId,
                        searchFilter,
                        skip: 0,
                        take: _repoSettings.SearchTake,
                        logger,
                        cancellationToken)).ToList();

                    foreach (IPackageSearchMetadata result in results)
                    {
                        if (result.Tags.Contains(_repoSettings.Tag))
                        {
                            var filename = Path.Combine(_repoSettings.NugetFolder, $"{result.Identity.Id}.{result.Identity.Version}.nupkg");

                            if (!File.Exists(filename))
                            {
                                _logger.LogInformation($"Found Remote package {result.Identity.Id} {result.Identity.Version}");

                                using (var packageStream = new MemoryStream())
                                {
                                    var success = await findPackageByIdResource.CopyNupkgToStreamAsync(
                                        result.Identity.Id, // package id
                                        result.Identity.Version,
                                        packageStream,
                                        cache,
                                        logger,
                                        cancellationToken);

                                    if (success)
                                    {
                                        var package = new PackageArchiveReader(packageStream);
                                        packages.Add(GenerateScript(filename, package));
                                    }

                                    await packageStream.FlushAsync();
                                    packageStream.Close();
                                }
                            }
                            else
                            {
                                _logger.LogDebug($"Ignoring {result.Identity.Id} version {result.Identity.Version} as we already have it");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error retrieving packages from {_repoSettings.GitRepo}");
                throw;
            }

            return packages;
        }

        private Package GenerateScript(string filename)
        {
            var importedDate = new FileInfo(filename).CreationTime;
            var package = new PackageArchiveReader(filename);
            return GenerateScript(filename, package, importedDate);
        }

        private Package GenerateScript(string filename, PackageArchiveReader packageArchiveReader, DateTime? importedDate = null)
        {
            var nuspec = packageArchiveReader.NuspecReader;
            
            var json = StreamToString(packageArchiveReader.GetStream("config.json"));
            var package = JsonConvert.DeserializeObject<SqlPackage>(json);
            
            package.Id = nuspec.GetId();
            package.Version = nuspec.GetVersion().OriginalVersion;
            package.ImportedDate = importedDate;
            package.Filename = filename;

            var scripts = new List<SimpleScript>();

            var extensions = new string[] { "*.sql", "*.ps1" };
            var scriptFiles = packageArchiveReader.GetFiles()
                                                  .Where(filename => extensions.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, filename)));

            foreach (var file in scriptFiles)
            {            
                var script = StreamToScript(file, packageArchiveReader.GetStream(GetFilename(file)));

                if (script is SqlScript sqlPackage && string.IsNullOrWhiteSpace(sqlPackage.ConnectionString))
                    sqlPackage.ConnectionString = package.ConnectionString;

                scripts.Add(script);
            }

            package.Scripts = scripts;

            return package;
        }

        private static string GetFilename(string file)
        {
            file = file.Replace("\\", "/");
            if (file.StartsWith("/"))
                file = file[1..];

            return file;
        }

        private static string StreamToString(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            reader.Close();

            return json;
        }

        private static SimpleScript StreamToScript(string filename, Stream stream)
        {
            var text = StreamToString(stream);

            switch (Path.GetExtension(filename).ToLower())
            {
                case ".sql":
                    return new SqlScript { Filename = filename, Script = text, ConnectionString = ConnectionString.GetConnectionFromFilePath(filename) };
                case ".ps1":
                    return new PowershellScript { Filename = filename, Script = text };
                default:
                    throw new ArgumentOutOfRangeException($"Unknown filetype for {filename}");
            }
        }

        public Task<string> ImportPackageAsync(Package package)
        {
            throw new NotImplementedException();
        }
    }
}
