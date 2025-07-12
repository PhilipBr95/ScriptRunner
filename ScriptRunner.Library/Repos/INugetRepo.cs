using NuGet.Protocol.Core.Types;
using ScriptRunner.Library.Models.Packages;

namespace ScriptRunner.Library.Repos
{
    public interface INugetRepo : IScriptRepo
    {
        Task ImportScriptsAsync(string user, string[] packageIds);
        Task<IEnumerable<Package>> ListScriptsAsync(string[]? packageIds = null);
    }
}