using Microsoft.Extensions.Options;
using System.CodeDom.Compiler;

namespace ScriptRunner.Library.Models
{
    public class Options
    {
        public string Layout { get; set; }
        public object[]? DataTables { get; set; }
        public IEnumerable<string>? Css { get; set; }
        public IEnumerable<JQuery>? JQuery { get; set; }

        /// <summary>
        /// (Optional) The OnClick URL template
        /// </summary>
        public IEnumerable<Column>? Columns { get; set; }

        /// <summary>
        /// Overriding text to use for the Results label
        /// </summary>
        public string? ResultsLabel { get; set; }        
        
        /// <summary>
        /// Overriding text to use for the Messages label
        /// </summary>
        public string? MessagesLabel { get; set; }

        /// <summary>
        /// Whether to check the results for Date columns
        /// </summary>
        public bool AutoFormatDates { get; set; } = true;

        /// <summary>
        /// Additional optional settings
        /// </summary>
        public Dictionary<string, string>? RunSettings { get; set; }
        
        /// <summary>
        /// A typed way of accessing the settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? GetSetting<T>(string key)
        {
            if (RunSettings?.ContainsKey(key) == true)
            {
                var type = typeof(T);

                if (type.IsGenericType)
                    type = type.GetGenericArguments()[0];

                return (T)Convert.ChangeType(RunSettings[key], type);
            }

            return default;
        }
    }
}