namespace ScriptRunner.UI.Services
{
    public class HistorySettings
    {
        public string Folder { get; set; }
        public string Filename => Path.Combine(Folder, "History.json");
        public string BackupFilename => Path.Combine(Folder, "History.Backup.json");
        public string DailyBackupFilename => Path.Combine(Folder, "History.{yyyyMMdd}.json");
    }
}