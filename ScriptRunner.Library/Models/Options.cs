﻿using Microsoft.Extensions.Options;
using System.CodeDom.Compiler;

namespace ScriptRunner.Library.Models
{
    public class Options
    {
        public Dictionary<string,string>? RunSettings { get; set; }
        public string? Layout { get; set; }
        public string? DataTableDom { get; set; }
        public IEnumerable<string>? Css { get; set; }
        public IEnumerable<JQuery>? JQuery { get; set; }
        public string? ResultsLabel { get; set; }
        public string? MessagesLabel { get; set; }

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