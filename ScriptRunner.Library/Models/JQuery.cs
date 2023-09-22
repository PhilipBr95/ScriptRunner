namespace ScriptRunner.Library.Models
{
    public class JQuery
    {
        public string Parent { get; set; }
        public string Selector { get; set; }
        public string Event { get; set; }
        public string Function { get; set; }
        public Dictionary<string,string>? Data { get; set; }
    }
}