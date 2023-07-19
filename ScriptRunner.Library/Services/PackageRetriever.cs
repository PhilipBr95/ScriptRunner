using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;

namespace ScriptRunner.Library.Services
{
    public class PackageRetriever : IPackageRetriever
    {
        private readonly IMemoryCache _scriptCache;
        private readonly ILogger<IPackageRetriever> _logger;
        private readonly IScriptRepo _scriptRepo;
        private readonly INugetRepo _nugetRepo;

        public PackageRetriever(IScriptRepo scriptRepo, INugetRepo nugetRepo, IMemoryCache memoryCache, ILogger<IPackageRetriever> logger) 
        {
            _scriptRepo = scriptRepo;
            _nugetRepo = nugetRepo;
            _scriptCache = memoryCache;
            _logger = logger;
        }

        public async Task<Package> GetPackageAsync(string packageId, string version)
        {
            var scripts = await GetPackagesAsync();
            return scripts.Where(w => w.Id == packageId && w.Version == version)
                               .Single();
        }

        public async Task<IEnumerable<Package>> GetPackagesAsync()
        {
            return await _scriptCache.GetOrCreateAsync(nameof(_scriptCache), async (cacheEntry) =>
            {
                var scripts = (await _scriptRepo.GetScriptsAsync()).ToList();
                scripts.AddRange(await _nugetRepo.GetScriptsAsync());

                return scripts;
            });
        }

        public void ClearPackageCache()
        {
            _logger?.LogInformation($"Clearing Local Package Cache");
            _scriptCache.Remove(nameof(_scriptCache));
        }

        public async Task ImportPackagesAsync(string user, string[] packageIds)
        {
            await _nugetRepo.ImportScriptsAsync(user, packageIds);

            ClearPackageCache();
            await GetPackagesAsync();
        }
    }
}
