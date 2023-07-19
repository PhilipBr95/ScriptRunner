using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuGet.Common;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public class PackageExecutor : IPackageExecutor
    {
        private readonly ISqlExecutor _sqlRunner;
        private readonly IPowerShellExecutor _powerShellRunner;
        private readonly ITransactionService _transactionService;
        private readonly IHistoryService _historyService;
        private readonly ILogger<IPackageExecutor> _logger;

        public PackageExecutor(ISqlExecutor sqlRunner, IPowerShellExecutor powerShellRunner, ITransactionService transactionService, 
                                IHistoryService historyService, ILogger<IPackageExecutor> logger)
        {
            _sqlRunner = sqlRunner;
            _powerShellRunner = powerShellRunner;
            _transactionService = transactionService;
            _historyService = historyService;
            _logger = logger;
        }

        public async Task<PackageResult> ExecuteAsync(Package package, string actionedBy)
        {
            string scriptFilename = string.Empty;
            bool success = true;

            try
            {
                _logger?.LogInformation($"Running {package.UniqueId} for {actionedBy}");
                                
                var scriptResults = new List<ScriptResults>();

                foreach (var script in package.Scripts)
                {
                    switch (script)
                    {
                        case SqlScript sqlScript:
                            scriptResults.Add(await _sqlRunner.ExecuteAsync(sqlScript, package.Params));
                            break;
                        case PowershellScript powershellScript:
                            scriptResults.Add(await _powerShellRunner.ExecuteAsync(powershellScript, package.Params));
                            break;
                    }
                }

                return new PackageResult { ScriptResults = scriptResults };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error running {package.UniqueId} - {scriptFilename} with Params: {JsonConvert.SerializeObject(package.Params)}");
                success = false;

                throw;
            }
            finally
            {
                await LogActivity(package, actionedBy, success);
            }
        }

        private async Task LogActivity(Package package, string actionedBy, bool success)
        {
            var activity = new Activity<Package>
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
