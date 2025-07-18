﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Models.Packages;
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

        public async Task<Package> GetPackageOrDefaultAsync(string uniqueId)
        {
            var scripts = await GetPackagesAsync();
            return scripts.Where(w => w.UniqueId == uniqueId)
                          .Single();
        }

        public async Task<IEnumerable<Package>> GetPackagesAsync()
        { 
            return await _scriptCache.GetOrCreateAsync(nameof(_scriptCache), async (cacheEntry) =>
            {
                List<Package> scripts = (await _scriptRepo.GetScriptsAsync()).ToList();
                scripts.AddRange(await _nugetRepo.GetScriptsAsync());

                //Find dups
                var dups = scripts.GroupBy(g => g.UniqueId)
                                  .Select(s => new { s.Key, Total = s.Count(), Files = s.Select(s => s.Filename) })
                                  .Where(w => w?.Total > 1)
                                  .Select(s => new { s.Key, s.Files });

                if (dups.Count() > 0)
                {
                    _logger?.LogWarning($"Removing: [{string.Join(",", dups.SelectMany(s => s.Files))}]");
                    _logger?.LogError($"{dups.Count()} Duplicate Packages found: [{string.Join(", ", dups.Select(s => s.Key))}]");                    

                    scripts.RemoveAll(r => dups.Select(s => s.Key).Contains(r.UniqueId) == true);
                }

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
