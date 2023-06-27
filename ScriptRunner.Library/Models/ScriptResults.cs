using System.Data;

namespace ScriptRunner.Library.Models
{
    public class ScriptResults
    {
        public IEnumerable<DataTable> DataTables { get; set; }
        public string Messages { get; set; }
    }
}