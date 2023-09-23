namespace ScriptRunner.UI.Services
{
    public class HistorySettings
    {
        public string Folder { get; set; }
        public string Filename => Path.Combine(Folder, "History.json");
        public string BackupFilename => Path.Combine(Folder, "History.Backup.json");
        public string DailyBackupFilename => Path.Combine(Folder, "History.{yyyyMMdd}.json");
        public string MonthlyBackupFilename => Path.Combine(Folder, "Monthly", "History.{yyyyMM}.json");
        public double MaxBackupAgeInDays { get; set; } = 100;
        public int MaxHistoryDaysOld { get; set; } = 400;
    }
}