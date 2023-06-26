using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IPackageExecutor
    {
        Task<IEnumerable<PackageResults>> ExecuteAsync(Package script, string actionedBy);
    }
}