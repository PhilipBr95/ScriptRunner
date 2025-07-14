using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Models.Packages
{
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
