using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IPackageExecutor
    {
        Task<PackageResult> ExecuteAsync(Package script, string actionedBy);
    }
}