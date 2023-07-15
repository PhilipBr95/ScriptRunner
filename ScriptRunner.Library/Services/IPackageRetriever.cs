using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IPackageRetriever
    {
        Task<IEnumerable<Package>> GetPackagesAsync();
        Task<Package> GetPackageAsync(string packageId, string version);
        void ClearPackageCache();
        Task ImportPackagesAsync(string user);
    }
}