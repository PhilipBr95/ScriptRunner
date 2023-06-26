namespace ScriptRunner.Library.Models
{
    public class NugetPackage : Package
    {
        public IEnumerable<string> Files { get; set; }
    }
}
