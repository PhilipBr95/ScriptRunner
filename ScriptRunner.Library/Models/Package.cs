﻿using System.Diagnostics;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Models
{
	[DebuggerDisplay("{UniqueId}")]
    public class Package
    {
        public string System { get; set; }
        public string Description { get; set; }
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
}
