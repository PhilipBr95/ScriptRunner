using System.Text.RegularExpressions;
using TSQL;

namespace ScriptRunner.Library.Models.Scripts
{
    public class SqlScript : SimpleScript
    {
        public SqlScript()
        {
            ScriptType = nameof(SqlScript);
        }

        public string ConnectionString { get; set; }

        public static string Parameterise(string sql, Param[] @params)
        {
            var updatedSql = Regex.Replace(sql, "DECLARE[\\s]+@([\\w]+)@.+", (match) =>
            {
                var declareSql = match.Value;
                var tokens = TSQLTokenizer.ParseTokens(declareSql);

                var varName = tokens[1].Text[1..^1];
                var param = @params.FirstOrDefault(f => f.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));

                if (param == null)
                    throw new ArgumentException($"Variable {varName} was not found in Param list");

                var paramValue = $"'{{{param.Name}}}'";
                int end = sql.Length;

                //Check for an existing value
                if (tokens[^2].Type == TSQL.Tokens.TSQLTokenType.Operator && tokens[^2].Text == "=")
                    end = tokens[^2].BeginPosition + 1;

                return $"{declareSql[0..end]} {paramValue} --Previously {declareSql[end..].Trim()}";
            });

            return updatedSql;
        }
    }
}