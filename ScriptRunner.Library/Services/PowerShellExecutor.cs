using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public class PowerShellExecutor : IPowerShellExecutor
    {               
        public async Task<PackageResults> ExecuteAsync(PowershellScript powershellScript, Param[] @params)
        {
            throw new NotImplementedException();
        }
    }
}