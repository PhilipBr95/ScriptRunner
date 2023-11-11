using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IPackageRetriever
    {
        Task<IEnumerable<Package>> GetPackagesAsync();
        Task<Package> GetPackageOrDefaultAsync(string uniqueId);
        void ClearPackageCache();
        Task ImportPackagesAsync(string user, string[] packageIds);
    }
}