﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Packaging;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Packages;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public class PackageExecutor : IPackageExecutor
    {
        private readonly ISqlExecutor _sqlRunner;
        private readonly IPowerShellExecutorResolver _powerShellExecutorResolver;
        private readonly ITransactionService _transactionService;
        private readonly IHistoryService _historyService;
        private readonly ILogger<IPackageExecutor> _logger;

        public PackageExecutor(ISqlExecutor sqlRunner, IPowerShellExecutorResolver powerShellExecutorResolver, ITransactionService transactionService, 
                                IHistoryService historyService, ILogger<IPackageExecutor> logger)
        {
            _sqlRunner = sqlRunner;
            _powerShellExecutorResolver = powerShellExecutorResolver;
            _transactionService = transactionService;
            _historyService = historyService;
            _logger = logger;
        }

        public async Task<PackageResult> ExecuteAsync(Package package, string actionedBy)
        {
            string scriptFilename = string.Empty;
            bool success = true;
            var scriptResults = new List<ScriptResults>();

            if (string.IsNullOrWhiteSpace(actionedBy))
                throw new Exception($"Unknown user running {package.UniqueId}");

            try
            {                
                _logger?.LogInformation($"Running {package.UniqueId} for {actionedBy}");                                            

                var parameters = package.Params?.ToList() ?? Array.Empty<Param>().ToList();
                parameters.Add(new Param { Name = "ActionedBy", Value = actionedBy });
                parameters.Add(new Param { Name = "ScriptFolder", Value = Path.GetDirectoryName(package.Filename) });

                foreach (var script in package.Scripts.OrderBy(o => Path.GetFileName(o.Filename)))
                {
                    switch (script)
                    {
                        case SqlScript sqlScript:
                            {
                                var results = await _sqlRunner.ExecuteAsync(sqlScript, parameters);
                                scriptResults.Add(results);
                            }
                            break;
                        case PowershellScript powershellScript:
                            {
                                var executor = _powerShellExecutorResolver.Resolve(package.Options);
                                var results = await executor.ExecuteAsync(powershellScript, parameters, package.Options);
                                scriptResults.Add(results);
                            }
                            break;
                    }
                }

                return new PackageResult { Results = scriptResults };
            }
            catch (Exception ex)
            {
                var paramstring = JsonConvert.SerializeObject(package.Params);
                if (paramstring.Length > 5000)
                    paramstring = paramstring[..5000] + "...";

                _logger?.LogError(ex, $"Unknown error running {package.UniqueId} - {scriptFilename} with Params: {paramstring}");
                success = false;

                throw;
            }
            finally
            {
                package.SetResults(scriptResults);
                await LogActivity(package, actionedBy, success);
            }
        }

        private async Task LogActivity(Package package, string actionedBy, bool success)
        {
            var activity = new ActivityWithData<Package>
            {
                System = package.System,
                Description = $"{package.Title} ({package.UniqueId})",
                Data = package,
                ActionedBy = actionedBy,
                Success = success
            };

            await _historyService.LogActivityAsync(activity);
            await _transactionService.LogActivityAsync(activity);
        }
    }
}
