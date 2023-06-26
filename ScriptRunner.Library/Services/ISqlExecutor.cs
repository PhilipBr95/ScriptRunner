using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public interface ISqlExecutor
    {
        Task<PackageResults> ExecuteAsync(SqlScript sqlScript, Param[] @params);
    }
}