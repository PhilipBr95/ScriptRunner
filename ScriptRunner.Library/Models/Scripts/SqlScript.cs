using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Models.Scripts
{
    public class SqlScript : SimpleScript
    {
        public string ConnectionString { get; set; }
    }
}