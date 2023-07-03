namespace ScriptRunner.UI.Services
{
    public class HistorySettings
    {
        public string Folder { get; set; }
        public string Filename => Path.Combine(Folder, "History.json");
        public string BackupFilename => Path.Combine(Folder, "History.backup");
    }
}