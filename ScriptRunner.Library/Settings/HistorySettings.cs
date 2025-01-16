namespace ScriptRunner.UI.Services
{
    public class HistorySettings
    {
        public string Folder { get; set; }
        public string Filename => Path.Combine(Folder, "History.json");
        public string BackupFilename => Path.Combine(Folder, "History.Backup.json");
        public string DailyBackupFilename => Path.Combine(Folder, "History.{yyyyMMdd}.json");
        public string MonthlyBackupFilename => Path.Combine(Folder, "Monthly", "History.{yyyyMM}.json");

        /// <summary>
        /// Max number of backup files
        /// </summary>
        public int MaxBackupFiles { get; set; } = 0;
        
        /// <summary>
        /// Max number of activities that will be kept in the History file
        /// </summary>
        public int MaxActivitiesInHistoryFile { get; set; } = 200;
        
        /// <summary>
        /// Number of minutes to cache the data
        /// </summary>
        public double CachingInMinutes { get; set; } = 30;
    }
}