using ScriptRunner.Library.Models.Packages;

namespace ScriptRunner.Library.Repos
{
    public interface IScriptRepo
    {
        Task<IEnumerable<Package>> GetScriptsAsync();
        Task<string> ImportPackageAsync(Package package);
    }
}
