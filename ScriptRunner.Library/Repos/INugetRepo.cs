using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Repos
{
    public interface INugetRepo : IScriptRepo
    {
        Task ImportScriptsAsync();
    }
}