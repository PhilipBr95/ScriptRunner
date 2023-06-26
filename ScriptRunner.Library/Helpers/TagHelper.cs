using ScriptRunner.Library.Models;
using System.Text.RegularExpressions;

namespace ScriptRunner.Library.Helpers
{
    internal class TagHelper
    {
        public static string PopulateTags(string scriptText, IEnumerable<Param> @params)
        {
            var regex = new Regex(@"{(\w+)}");
            return regex.Replace(scriptText, (e) => e.Value.Replace(e.Groups[0].Value, @params.Single(s => s.Name == e.Groups[1].Value).Value));
        }
    }
}
