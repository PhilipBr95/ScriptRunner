using System.Data;

namespace ScriptRunner.Library.Models
{
    public class PackageResult
    {
        public IEnumerable<ScriptResults> ScriptResults { get; set; }
    }
}