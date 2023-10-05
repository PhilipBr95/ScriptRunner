using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Settings;

namespace ScriptRunner.Library.Services
{
    public class PowerShellExecutorResolver : IPowerShellExecutorResolver
    {
        private readonly PowershellSettings _powershellSettings;
        private readonly IEnumerable<IPowerShellExecutor> _powerShellExecutors;
        private readonly ILogger<IPowerShellExecutorResolver> _logger;

        public PowerShellExecutorResolver(IOptions<PowershellSettings> options, IEnumerable<IPowerShellExecutor> powerShellExecutors, ILogger<IPowerShellExecutorResolver> logger)
        {
            _powershellSettings = options.Value;
            _powerShellExecutors = powerShellExecutors;
            _logger = logger;
        }

        public IPowerShellExecutor Resolve(Models.Options? options)
        {
            var executor = options?.GetSetting<string?>("Executor") ?? _powershellSettings.DefaultExecutor;

            try
            {                               
                return _powerShellExecutors.Where(w => w.GetType().Name == executor).Single();
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Failed to find {executor} in {string.Join(",", _powerShellExecutors?.Select(s => s.GetType().Name))}");
                throw;
            }
        }
    }
}
