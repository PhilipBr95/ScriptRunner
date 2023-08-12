using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Models
{
	[DebuggerDisplay("{UniqueId}")]
    public class Package
    {
        public string System { get; set; }
        public string Description { get; set; }
        [JsonConverter(typeof(SimpleScriptJsonConverter))]
        public IEnumerable<SimpleScript> Scripts { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string Title { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public DateTime? ImportedDate { get; set; }
        public Param[] Params { get; set; }
        public string UniqueId => $"{Id} - {Version}";

        public void PopulateParams(Package script)
        {
            foreach(var param in Params)
            {
                param.Value = script.Params.Single(s => s.Name == param.Name && s.Type == param.Type).Value;
            }
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

                switch (scriptType.ToString())
                {
                    case "SqlScript":
                        scripts.Add(jObject.ToObject<SqlScript>());
                        break;
                    case "PowershellScript":
                        scripts.Add(jObject.ToObject<PowershellScript>());
                        break;
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
