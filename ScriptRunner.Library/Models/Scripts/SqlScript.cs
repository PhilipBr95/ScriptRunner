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
        public bool NoOutput { get; set; } = false;

        public static string Parameterise(string sql, Param[] @params)
        {
            var declareCounter = 0;

            var updatedSql = Regex.Replace(sql, "DECLARE[\\s]+@([\\w]+)@.+", (match) =>
            {
                declareCounter++;

                var declareSql = match.Value;
                var tokens = TSQLTokenizer.ParseTokens(declareSql);

                var varName = tokens[1].Text[1..^1];
                var param = @params?.FirstOrDefault(f => f.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));

                if (param == null)
                    throw new ArgumentException($"Variable {varName} was not found in Param list");
               
                var paramName = $"'{{{param.Name}}}'";
                int end = declareSql.Length;

                if(tokens.Count < 3)
                    throw new ArgumentException($"DECLARE statement looks wrong - {declareSql}");

                //Find the equals
                var equals = tokens.Where(w => w.Type == TSQL.Tokens.TSQLTokenType.Operator && w.Text == "=")
                                   .FirstOrDefault();

                if(equals == null)
                {
                    if (tokens[^1].Type == TSQL.Tokens.TSQLTokenType.SingleLineComment)
                        end = tokens[^2].EndPosition + 1;
                    else
                        end = tokens[^1].EndPosition + 1;
                }
                else
                    end = equals.EndPosition - 1;


                var previously = declareSql[end..].Trim();

                if (previously.Length > 0)
                    previously = $" --Previously {previously}";
                
                return $"{declareSql[0..end]} = {paramName}{previously}";
            });

            if(@params != null && declareCounter > @params.Count())
                throw new ArgumentException($"Parameter count({declareCounter}) does not match the script Variable count({@params.Count()})");

            return updatedSql;
        }
    }
}