using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Models.Scripts
{
    public class SqlScript : SimpleScript
    {
        public SqlScript()
        {
            ScriptType = nameof(SqlScript);
        }

        public string ConnectionString { get; set; }
    }
}