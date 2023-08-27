
using ScriptRunner.Library.Models;
using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Helpers
{
    internal class TagHelper
    {
        public static string PopulateTags(string scriptText, IEnumerable<Param> @params)
        {
            foreach(var param in @params)
            {
                var regex = new Regex($"{{{param.Name}}}");
                scriptText = regex.Replace(scriptText, param.Value ?? string.Empty);
            }

            return scriptText;
        }
    }
}
