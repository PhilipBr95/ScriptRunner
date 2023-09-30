using Microsoft.Extensions.Logging;
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
            var declareCounter = 0;

            var updatedSql = Regex.Replace(sql, "DECLARE[\\s]+@([\\w]+)@.+", (match) =>
            {
                declareCounter++;

                var declareSql = match.Value;
                var tokens = TSQLTokenizer.ParseTokens(declareSql);

                var varName = tokens[1].Text[1..^1];
                var param = @params.FirstOrDefault(f => f.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));

                if (param == null)
                    throw new ArgumentException($"Variable {varName} was not found in Param list");
               
                var paramName = $"'{{{param.Name}}}'";
                int end = declareSql.Length;

                if(tokens.Count < 3)
                    throw new ArgumentException($"DECLARE statement looks wrong - {declareSql}");

                //We only want the first 3 tokens
                end = tokens[2].EndPosition + 1;

                var previously = tokens.Count == 3 ? string.Empty : $" --Previously {declareSql[end..].Trim()}";

                return $"{declareSql[0..end]} = {paramName}{previously}";
            });

            if(declareCounter != @params.Count())
                throw new ArgumentException($"Parameter count({declareCounter}) does not match the script Variable count({@params.Count()})");

            return updatedSql;
        }
    }
}