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

        public async Task<IEnumerable<PackageResults>> ExecuteAsync(Package package, string actionedBy)
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

                var results = new List<PackageResults>();

                foreach (var script in package.Scripts)
                {
                    switch (script)
                    {
                        case SqlScript sqlScript:
                            results.Add(await _sqlRunner.ExecuteAsync(sqlScript, package.Params));
                            break;
                        case PowershellScript powershellScript:
                            results.Add(await _powerShellRunner.ExecuteAsync(powershellScript, package.Params));
                            break;
                    }
                }

                return results;
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error running {package.UniqueId} - {scriptFilename} with Params: {JsonConvert.SerializeObject(package.Params)}");
                throw;
            }
        }
    }
}
