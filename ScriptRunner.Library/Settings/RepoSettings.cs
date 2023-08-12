namespace ScriptRunner.Library.Settings
{
    public class RepoSettings
    {
        public string NugetFolder { get; set; }
        public string ScriptFolder { get; set; }
        public string GitRepo { get; set; }
        public string Tag { get; set; }
        public int SearchTake { get; set; } = 9999;
        public string TemporaryScriptFolder { get; set; }
    }
}
