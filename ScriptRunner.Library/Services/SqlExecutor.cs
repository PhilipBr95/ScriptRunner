using Microsoft.Extensions.Logging;
using NuGet.Common;
using ScriptRunner.Library.Helpers;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace ScriptRunner.Library.Services
{
    public class SqlExecutor : ISqlExecutor
    {
        private readonly ILogger<ISqlExecutor> _logger;
        
        public SqlExecutor(ILogger<ISqlExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<ScriptResults> ExecuteAsync(SqlScript sqlScript, Param[] @params)
        {
            try
            {
                var results = new List<DataTable>();
                var messages = new List<string>();

                using var conn = new SqlConnection(sqlScript.ConnectionString);
                conn.InfoMessage += (sender, e) => messages.Add(e.Message);

                await conn.OpenAsync();
                var sql = TagHelper.PopulateTags(sqlScript.Script, @params);

                _logger?.LogInformation($"Running: {sqlScript.Filename} - {sql}");

                var command = new SqlCommand(sql, conn);

                var reader = await command.ExecuteReaderAsync();

                while (reader.IsClosed == false && reader.HasRows)
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    results.Add(dataTable);
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