using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public interface IPowerShellExecutor
    {
        Task<PackageResults> ExecuteAsync(PowershellScript powershellScript, Param[] @params);
    }
}