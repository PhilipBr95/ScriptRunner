using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public interface ISqlExecutor
    {
        Task<ScriptResults> ExecuteAsync(SqlScript sqlScript, Param[] @params);
    }
}