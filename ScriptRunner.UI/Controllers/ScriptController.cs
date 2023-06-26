using Microsoft.AspNetCore.Mvc;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScriptController : Controller
    {
        private readonly IPackageRetriever _scriptRetriever;
        private readonly IPackageExecutor _scriptRunner;
        private readonly ILogger<ScriptController> _logger;

        public ScriptController(IPackageRetriever scriptRetriever, IPackageExecutor scriptRunner, ILogger<ScriptController> logger) 
        {
            _scriptRetriever = scriptRetriever;
            _scriptRunner = scriptRunner;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetScriptsAsync()
        {
            try
            {
                //todo - create a ScriptVM to remove some properties
                var scripts = await _scriptRetriever.GetPackagesAsync();
                return Json(scripts);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "");
                return Json(null);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RunScript(Package script)
        {
            //Play it safe and get the Script again :-)
            var repoScript = await _scriptRetriever.GetPackageAsync(script.Id, script.Version);
            repoScript.PopulateParams(script);
            
            var currentUser = HttpContext.User.Identity.Name;
            var result = await _scriptRunner.ExecuteAsync(repoScript, currentUser);
            return Ok(result);
        }
    }
}
