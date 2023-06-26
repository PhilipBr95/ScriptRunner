namespace ScriptRunner.Library.Models
{
    public class ConnectionString
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string TrustedConnection => $"Server={Server};Database={Database};Trusted_Connection=True;";

        public static string GetConnectionFromFilePath(string filePath)
        {
            var parts = filePath.Split(Path.DirectorySeparatorChar);
            return new ConnectionString { Server = parts[1], Database = parts[2] }.TrustedConnection;
        }
    }
}
