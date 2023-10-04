using ScriptRunner.Library.Services;

namespace ScriptRunner.Library.Settings
{
    public class PowershellSettings
    {
        public string DefaultExecutor { get; set; } = nameof(PowerShellCoreExecutor);
        public string Executable { get; set; } = "powershell.exe";
        public string ExecutableArguments { get; set; } = $"-NoProfile -ExecutionPolicy ByPass";
        public bool UseShellExecute { get; set; } = false;
        public bool RedirectStandardOutput { get; set; } = true;
        public bool RedirectStandardError { get; set; } = true;
        public string NewLine { get; set; } = "\n";
        public bool UseTemporaryFile { get; set; } = true;
        public string TempFolder { get; set; } = "C:\\Nuget\\ScriptRunner\\Temp";
    }
}
