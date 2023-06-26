namespace ScriptRunner.Library.Settings
{
    public class RepoSettings
    {
        public string NugetFolder { get; set; }
        public string ScriptFolder { get; set; }
        public string GitRepo { get; set; }
        public string Tags { get; set; }
        public int SearchTake { get; set; } = 9999;
    }
}
