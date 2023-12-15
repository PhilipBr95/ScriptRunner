using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Repos
{
    public interface IScriptRepo
    {
        Task<IEnumerable<Package>> GetScriptsAsync();
        Task<string> ImportPackageAsync(Package package);
    }
}
