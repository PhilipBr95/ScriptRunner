using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Models
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

		public string UniqueId => $"{Category}.{System}: {Id} - {Version}";
        public IEnumerable<ScriptResults>? Results { get; set; } = null;
        public Options? Options { get; set; }

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

    public class SimpleScriptJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var scripts = new List<SimpleScript>();

            //Move the pointer onto the array
            //reader.Read();

            if (reader.TokenType != JsonToken.StartArray)
                throw new Exception("Where's the array?");

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                var scriptType = jObject.Property("ScriptType", StringComparison.OrdinalIgnoreCase);
                var scriptTypeValue = scriptType.Value.Value<string>();

                switch (scriptTypeValue)
                {
                    case "SqlScript":
                        scripts.Add(jObject.ToObject<SqlScript>());
                        break;
                    case "PowershellScript":
                        scripts.Add(jObject.ToObject<PowershellScript>());
                        break;
                    default:
                        throw new ArgumentException($"Unknown ScriptType: {scriptTypeValue}");
                }
            }

            return scripts;
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }

}
