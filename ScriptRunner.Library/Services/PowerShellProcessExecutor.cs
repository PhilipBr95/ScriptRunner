using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public async Task<ScriptResults> ExecuteAsync(PowershellScript powershellScript, Param[] @params, Models.Options? options)
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
                    error = proc.StandardOutput.ReadToEnd();

                await proc.WaitForExitAsync();

                if(!string.IsNullOrWhiteSpace(error))
                    throw new Exception(error);

                var detectTables = options?.GetSetting<bool?>("Powershell.DetectTables") ?? _powershellSettings.DetectTables;

                List<DataTable>? dataTables = null;
                List<string>? messages = null;

                var lines = output.Split(_powershellSettings.NewLine);

                if (detectTables)
                    (dataTables, messages) = FindTables(lines);
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

        private static (List<DataTable> dataTables, List<string> messages) FindTables(string[] lines)
        {
            var dataTables = new List<DataTable>();
            var messages = new List<string>();

            DataTable? table = null;
            IEnumerable<Column>? cols = null;
            var inTable = false;           

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var chars = line.Replace(" ", string.Empty).Replace("\r", string.Empty).Distinct();

                if (chars.Count() == 1 && chars.First() == '-')
                {
                    messages.RemoveAt(messages.Count - 1);

                    inTable = true;

                    table = new DataTable();
                    dataTables.Add(table);

                    var propLine = lines[i - 1];
                    cols = GetColumns(propLine);

                    foreach (var col in cols)
                    {
                        table.Columns.Add(col.ColumnName, typeof(string));
                    }
                }
                else if (line == "\r")
                {
                    inTable = false;
                }
                else if (inTable)
                {
                    var row = new List<string>();

                    foreach (var col in cols)
                    {
                        var value = line[col.StartPosition..col.EndPosition].Trim();
                        row.Add(value);
                    }

                    table!.Rows.Add(row.ToArray());
                }
                else
                {
                    messages.Add(line);
                }
            }

            return (dataTables, messages);
        }

        private static IEnumerable<Column> GetColumns(string propLine)
        {
            var columns = new List<Column>();
            Column? col = null;
            var start = 0;
            var inColumn = false;

            for (var i=0; i < propLine.Length; i++)
            {
                if (propLine[i] == ' ' && inColumn == false)
                {
                    inColumn = true;
                    col = new Column { StartPosition = start, ColumnName = propLine[start..i] };
                    columns.Add(col);

                    start = i;
                }            
                else if (propLine[i] != ' ' && inColumn == true)
                {
                    col!.EndPosition = i;
                    inColumn = false;
                    start = i;
                }
            }

            return columns;
        }
    }
}
