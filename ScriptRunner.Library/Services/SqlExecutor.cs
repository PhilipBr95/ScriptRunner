using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using ScriptRunner.Library.Helpers;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using System.Data;

namespace ScriptRunner.Library.Services
{
    public class SqlExecutor : ISqlExecutor
    {
        private readonly ILogger<ISqlExecutor> _logger;
        
        public SqlExecutor(ILogger<ISqlExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<ScriptResults> ExecuteAsync(SqlScript sqlScript, IEnumerable<Param> @params)
        {
            try
            {
                var results = new List<DataTable>();
                var messages = new List<string>();
                                
                var sql = TagHelper.PopulateTags(sqlScript.Script, @params);

                _logger?.LogInformation($"Running: {sqlScript.Filename} with [{sqlScript.ConnectionString}] - {sql}");

                using var conn = new SqlConnection(sqlScript.ConnectionString);
                await conn.OpenAsync();
                conn.InfoMessage += (sender, e) => messages.Add(e.Message);

                Server server = new Server(new ServerConnection(conn));

                //Do we need to record the output
                if (sqlScript.NoOutput)
                {
                    var recordCount = server.ConnectionContext.ExecuteNonQuery(sql);
                    messages.Add($"{recordCount} records returned");
                }
                else
                {
                    var reader = server.ConnectionContext.ExecuteReader(sql);

                    while (reader.IsClosed == false && reader.HasRows)
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        results.Add(dataTable);
                    }
                }

                return new ScriptResults { DataTables = results, Messages = messages };
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error running {sqlScript.Filename}");
                throw;
            }
        }
    }
}