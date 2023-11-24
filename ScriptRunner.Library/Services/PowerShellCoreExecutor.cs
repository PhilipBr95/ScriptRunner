using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Helpers;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;

namespace ScriptRunner.Library.Services
{
    public class PowerShellCoreExecutor : IPowerShellExecutor
    {
        private readonly ILogger<IPowerShellExecutor> _logger;

        public PowerShellCoreExecutor(ILogger<IPowerShellExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<ScriptResults> ExecuteAsync(PowershellScript powershellScript, IEnumerable<Param> @params, Models.Options options)
        {
            try
            {
                var script = TagHelper.PopulateTags(powershellScript.Script, @params);

                var powershell = PowerShell.Create();
                powershell.AddScript(script);


                if(@params != null && @params.Count() > 0)
                    powershell.AddParameters(@params.Select(s => new { s.Name, s.Value })
                                                    .ToDictionary(k => k.Name));

                _logger?.LogInformation($"Running: {powershellScript.Filename} - {script}");

                Collection<PSObject> results = powershell.Invoke();                
                IEnumerable<string> currentProps = new List<string>();

                DataTable table = null;
                var dataTables = new List<DataTable>();

                foreach (var result in results)
                {
                    var props = GetProperties(result);

                    //Have we moved to a new datatype
                    if(string.Join(",", props) != string.Join(",", currentProps))
                    {
                        currentProps = props;

                        table = new DataTable();
                        dataTables.Add(table);

                        foreach (var prop in props)
                        {
                            table.Columns.Add(prop, typeof(string));                         
                        }
                    }

                    var row = result.Properties.Where(w => currentProps.Contains(w.Name))
                                               .Select(s => s.Value?.ToString())
                                               .ToArray();
                    table?.Rows?.Add(row);
                }

                var errors = powershell.Streams.Error.Select(s => s.Exception.ToString())
                                                     .ToArray();

                if (errors.Count() > 0)
                    throw new Exception(errors.First());

                var messages = powershell.Streams.Information.Select(s => s.MessageData.ToString())
                                                             .ToArray();

                return new ScriptResults { DataTables = dataTables, Messages = messages };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error running the Powershell {powershellScript.Filename}");
                throw;
            }
        }

        private IEnumerable<string> GetProperties(PSObject result)
        {
            var ignore = new string[] { "BaseName", "VersionInfo" };
            var props = new List<string>();

            for (int i = 0; i < result.Properties.Count(); i++)
            {
                var prop = result.Properties.ElementAt(i);

                if (ignore.Contains(prop.Name))
                    continue;

                props.Add(prop.Name);
            }

            return props;
        }
    }
}