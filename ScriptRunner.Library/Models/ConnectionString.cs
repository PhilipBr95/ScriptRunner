namespace ScriptRunner.Library.Models
{
    public class ConnectionString
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string TrustedConnection => $"Server={Server};Database={Database};Trusted_Connection=True;";

        public static string GetConnectionFromFilePath(string filePath)
        {
            var parts = Path.GetDirectoryName(filePath)
                            .Split(Path.DirectorySeparatorChar);
            if(parts.Length > 1)
                return new ConnectionString { Server = parts[^2], Database = parts[^1] }.TrustedConnection;

            return null;
        }
    }
}
