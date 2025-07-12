using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Packages;

namespace ScriptRunner.Library.Services
{
    public interface IPackageExecutor
    {
        Task<PackageResult> ExecuteAsync(Package script, string actionedBy);
    }
}