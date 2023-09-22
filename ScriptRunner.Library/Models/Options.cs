namespace ScriptRunner.Library.Models
{
    public class Options
    {
        public string? Layout { get; set; }
        public string? DataTableDom { get; set; }
        public IEnumerable<string>? Css { get; set; }
        public IEnumerable<JQuery>? JQuery { get; set; }
    }
}