using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Models.Scripts
{
    public class SqlScript : BaseScript
    {
        public string ConnectionString { get; set; }
    }
}