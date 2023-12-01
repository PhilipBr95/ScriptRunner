using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ScriptRunner.Library.Helpers;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Settings;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Services
{
    public class PowerShellProcessExecutor : IPowerShellExecutor
    {
        private readonly PowershellSettings _powershellSettings;
        private readonly ILogger<IPowerShellExecutor> _logger;

        public PowerShellProcessExecutor(IOptions<PowershellSettings> options, ILogger<IPowerShellExecutor> logger)
        {
            _powershellSettings = options.Value;
            _logger = logger;
        }

        public async Task<ScriptResults> ExecuteAsync(PowershellScript powershellScript, IEnumerable<Param> @params, Models.Options? options)
        {
            var psCommand = string.Empty;
            var tempFile = string.Empty;

            try
            {
                var script = TagHelper.PopulateTags(powershellScript.Script, @params);
                var useTempFile = options?.GetSetting<bool?>("Powershell.UseTemporaryFile") ?? _powershellSettings.UseTemporaryFile;
                
                if (useTempFile)
                {
                    if (!Directory.Exists(_powershellSettings.TempFolder))
                        Directory.CreateDirectory(_powershellSettings.TempFolder);

                    tempFile = Path.Combine(_powershellSettings.TempFolder, $"{Path.GetRandomFileName()}.ps1");
                    File.WriteAllText(tempFile, script);

                    psCommand = $" -File \"{tempFile}\"";
                }
                else
                {
                    //EncodedCommands don't appear to work with Redirects :-(
                    _logger?.LogWarning("EncodedCommands don't appear to work with Redirects");

                    var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(script);
                    var psCommandBase64 = Convert.ToBase64String(psCommandBytes);

                    psCommand = $" -EncodedCommand {psCommandBase64}";
                }

                _logger?.LogInformation($"Running: {powershellScript.Filename} {psCommand}");

                var startInfo = new ProcessStartInfo()
                {
                    FileName = options?.GetSetting<string?>("Powershell.Executable") ?? _powershellSettings.Executable,
                    Arguments = (options?.GetSetting<string?>("Powershell.ExecutableArguments") ?? _powershellSettings.ExecutableArguments) + psCommand,
                    UseShellExecute = options?.GetSetting<bool?>("Powershell.UseShellExecute") ?? _powershellSettings.UseShellExecute,
                    RedirectStandardOutput = options?.GetSetting<bool?>("Powershell.RedirectStandardOutput") ?? _powershellSettings.RedirectStandardOutput,
                    RedirectStandardError = options?.GetSetting<bool?>("Powershell.RedirectStandardError") ?? _powershellSettings.RedirectStandardError
                };

                var proc = Process.Start(startInfo)!;
                var output = string.Empty;
                var error = string.Empty;

                if (startInfo.RedirectStandardOutput)
                    output = proc.StandardOutput.ReadToEnd();

                if (startInfo.RedirectStandardError)
                    error = proc.StandardError.ReadToEnd();

                await proc.WaitForExitAsync();

                if(!string.IsNullOrWhiteSpace(error))
                    throw new Exception(error);

                var detectJsonTables = options?.GetSetting<bool?>("Powershell.ConvertJsonToTable") ?? _powershellSettings.DetectJsonTables;

                List<DataTable>? dataTables = null;
                List<string>? messages = null;

                var lines = output.Split(_powershellSettings.NewLine);

                if (detectJsonTables)
                    (dataTables, messages) = FindJsonTables(lines);
                else
                    messages = lines.ToList();

                return new ScriptResults { DataTables = dataTables, Messages = messages };
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Error running the Powershell {psCommand}");
                throw;
            }
            finally
            {
                if(!string.IsNullOrWhiteSpace(tempFile) && File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private static (List<DataTable> dataTables, List<string> messages) FindJsonTables(string[] lines)
        {
            //Find all the []'s
            var regex = new Regex("\\[(.*?)]");
            var datasets = regex.Matches(string.Join(" ", lines));

            var messages = new List<string>();
            List<DataTable> dataTables = datasets.Select(s => (DataTable)JsonConvert.DeserializeObject(s.Value, (typeof(DataTable))))
                                                 .ToList();                            

            return (dataTables, messages);
        }

    }
}
