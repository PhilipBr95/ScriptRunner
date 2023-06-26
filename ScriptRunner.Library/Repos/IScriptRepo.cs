using ScriptRunner.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptRunner.Library.Repos
{
    public interface IScriptRepo
    {
        Task<IEnumerable<Package>> GetScriptsAsync();     
    }
}
