using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ScriptRunner.Library.Services
{
    public class MemoryLog
    {
        public string CategoryName { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; }
    }
}