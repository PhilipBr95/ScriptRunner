﻿using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;

namespace ScriptRunner.Library.Services
{
    public interface IPowerShellExecutor
    {
        /// <summary>
        /// Executes the Powershell
        /// </summary>
        /// <param name="powershellScript"></param>
        /// <param name="params"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<ScriptResults> ExecuteAsync(PowershellScript powershellScript, IEnumerable<Param> @params, Options options);
    }
}