using System.Diagnostics;
using Newtonsoft.Json;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Models.Packages
{
    [DebuggerDisplay("{UniqueId}")]
    public class Package
    {
        public string Filename { get; set; }
        public string Category { get; set; }
        public string System { get; set; }
        public string Description { get; set; }
        [JsonConverter(typeof(SimpleScriptJsonConverter))]
        public IEnumerable<SimpleScript> Scripts { get; set; }
        public IEnumerable<string>? Tags { get; set; }
        public string Title { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public DateTime? ImportedDate { get; set; }
        public Param[]? Params { get; set; }
        public string[]? AllowedADGroups { get; set; }
        public bool RunAsUser { get; set; } = false;
        public string UniqueId => $"{Category}.{System}: {Id} - {Version}";
        public IEnumerable<ScriptResults>? Results { get; set; } = null;
        public Options? Options { get; set; }

        /// <summary>
        /// Whether to automatically execute
        /// </summary>
        public bool AutoExecute { get; set; } = false;

        public Package CloneWithParams(Package script)
        {
            var newScript = JsonConvert.DeserializeObject<Package>(JsonConvert.SerializeObject(script));

            if (newScript.Params != null)
            {
                foreach (var param in newScript.Params)
                {
                    param.Value = script.Params.Single(s => s.Name == param.Name && s.Type == param.Type).Value;
                }
            }

            return newScript;
        }

        internal void SetResults(IEnumerable<ScriptResults> results)
        {
            Results = results;
        }
    }

}
