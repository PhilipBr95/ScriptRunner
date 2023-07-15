using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using System.IO;
using System.Text;

namespace ScriptRunner.Library.Repos
{
    public class NugetRepo : INugetRepo
    {
        private readonly ITransactionService _transactionService;
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<INugetRepo> _logger;

        public NugetRepo(ITransactionService transactionService, IOptions<RepoSettings> options, ILogger<INugetRepo> logger) : this(transactionService, options.Value, logger)
        {            
        }

        public NugetRepo(ITransactionService transactionService, RepoSettings repoSettings, ILogger<INugetRepo> logger)
        {
            _transactionService = transactionService;
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
                parsedScripts.Add(GenerateScript(file));
            }

            return parsedScripts;
        }

        public async Task ImportScriptsAsync(string user)
        {
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

                var results = (await packageSearchResource.SearchAsync(
                    "", // search string
                    searchFilter,
                    skip: 0,
                    take: _repoSettings.SearchTake,
                    logger,
                    cancellationToken)).ToList();

                foreach (IPackageSearchMetadata result in results)
                {
                    if (result.Tags.Contains(_repoSettings.Tags))
                    {
                        _logger.LogInformation($"Found package {result.Identity.Id} {result.Identity.Version}");
                        var filename = Path.Combine(_repoSettings.NugetFolder, $"{result.Identity.Id}.{result.Identity.Version}.nupkg");

                        if (!File.Exists(filename))
                        {
                            //Wipe the old versions
                            Directory.GetFiles(_repoSettings.NugetFolder, $"{result.Identity.Id}.*.nupkg")
                                     .ToList()
                                     .ForEach(fe => File.Delete(fe));

                            using (var packageStream = File.OpenWrite(filename))
                            {
                                var success = await findPackageByIdResource.CopyNupkgToStreamAsync(
                                    result.Identity.Id, // package id
                                    result.Identity.Version,
                                    packageStream,
                                    cache,
                                    logger,
                                    cancellationToken);

                                await packageStream.FlushAsync();
                                packageStream.Close();

                                _logger.LogInformation($"Downloaded {result.Identity.Id} version {result.Identity.Version} - {success}");
                                await _transactionService.LogActivityAsync(new Activity<Param[]> { ActionedBy = user, System = "ScriptRunner", Success = true, Description = $"Package imported - {result.Identity.Id} version {result.Identity.Version}" });
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Already have {result.Identity.Id} version {result.Identity.Version}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error retrieving packages from {_repoSettings.GitRepo}");
                throw;
            }
        }

        private Package GenerateScript(string filename)
        {
            var package = new NuGet.Packaging.PackageArchiveReader(filename);
            var nuspec = package.NuspecReader;

            var json = StreamToString(package.GetStream("config.json"));
            var nugetPackage = JsonConvert.DeserializeObject<NugetPackage>(json);

            nugetPackage.Id = nuspec.GetId();
            nugetPackage.Version = nuspec.GetVersion().OriginalVersion;
            nugetPackage.CreationTime = new FileInfo(filename).CreationTime;

            var scripts = new List<SimpleScript>();

            foreach(var file in nugetPackage.Files)
            {
                scripts.Add(StreamToScript(file, package.GetStream(GetFilename(file))));
            }

            nugetPackage.Scripts = scripts;

            return nugetPackage;
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
    }
}
