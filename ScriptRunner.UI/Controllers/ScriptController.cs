using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptController : Controller
    {
        private readonly IPackageRetriever _scriptRetriever;
        private readonly IPackageExecutor _scriptRunner;
        private readonly INugetRepo _nugetRepo;
        private readonly ILogger<ScriptController> _logger;

        public ScriptController(IPackageRetriever scriptRetriever, IPackageExecutor scriptRunner, INugetRepo nugetRepo, ILogger<ScriptController> logger) 
        {
            _scriptRetriever = scriptRetriever;
            _scriptRunner = scriptRunner;
            _nugetRepo = nugetRepo;
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
                _logger?.LogError(ex, "Unknown error");
                return Json(null);
            }
        }

        public class PackageModel
        {
            public string System { get; set; }
            public string Title{ get; set; }

            public string Id { get; set; }
            public string Version { get; set; }
            public DateTime? CreationTime { get; set; }

            public PackageModel(IPackageSearchMetadata s)
            {
                Version = s.Identity.Version.OriginalVersion;
                Title = s.Title;
                Id = s.Identity.Id;
            }
        }

        [HttpGet("/api/[controller]/remote")]
        public async Task<IActionResult> GetRemoteScriptsAsync()
        {
            try
            {                
                //todo - create a ScriptVM to remove some properties
                var scripts = await _nugetRepo.ListScriptsAsync();
                return Json(new { data = scripts });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unknown error");
                return Json(null);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RunScript(Package script)
        {
            try
            {
                //Play it safe and get the Script again :-)
                var repoScript = await _scriptRetriever.GetPackageAsync(script.Id, script.Version);
                repoScript.PopulateParams(script);

                var currentUser = HttpContext.User.Identity.Name;
                var result = await _scriptRunner.ExecuteAsync(repoScript, currentUser);
                return Ok(result);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error with {script.UniqueId}");
                throw;
            }
        }
    }
}
