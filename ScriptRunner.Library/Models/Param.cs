using System.ComponentModel;
using System.Diagnostics;

namespace ScriptRunner.Library.Models
{
	[DebuggerDisplay("{Name}={Value}")]
    public class Param
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public bool Required { get; set; } = true;
        public string HtmlType => new Services.TypeConverter(Type).ConvertToHtml();
        public string? Tooltip { get; set; }
    }
}
