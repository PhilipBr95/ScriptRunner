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
        private readonly ITransactionService _transaction;
        private readonly ILogger<IPackageExecutor> _logger;

        public PackageExecutor(ISqlExecutor sqlRunner, IPowerShellExecutor powerShellRunner, ITransactionService transaction, ILogger<IPackageExecutor> logger)
        {
            _sqlRunner = sqlRunner;
            _powerShellRunner = powerShellRunner;
            _transaction = transaction;
            _logger = logger;
        }

        public async Task<PackageResult> ExecuteAsync(Package package, string actionedBy)
        {
            string scriptFilename = string.Empty;

            try
            {
                _logger?.LogInformation($"Running {package.UniqueId} for {actionedBy}");

                _transaction.LogActivity(new Activity<Param[]> { 
                    System = package.System, 
                    Description = $"{package.Title} ({package.UniqueId})", 
                    Data = package.Params, 
                    ActionedBy = actionedBy 
                });

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
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error running {package.UniqueId} - {scriptFilename} with Params: {JsonConvert.SerializeObject(package.Params)}");
                throw;
            }
        }
    }
}
