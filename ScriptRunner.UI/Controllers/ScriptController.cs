using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.Types;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Models.Scripts;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using ScriptRunner.UI.Extensions;
using ScriptRunner.UI.Settings;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptController : Controller
    {
        private readonly IPackageRetriever _scriptRetriever;
        private readonly IPackageExecutor _scriptRunner;
        private readonly INugetRepo _nugetRepo;
        private readonly IScriptRepo _scriptRepo;
        private readonly RepoSettings _repoSettings;
        private readonly WebSettings _webSettings;
        private readonly ILogger<ScriptController> _logger;

        public ScriptController(IPackageRetriever scriptRetriever, IPackageExecutor scriptRunner, INugetRepo nugetRepo, IScriptRepo scriptRepo, IOptions<RepoSettings> options, IOptions<WebSettings> webSettings, ILogger<ScriptController> logger) 
        {
            _scriptRetriever = scriptRetriever;
            _scriptRunner = scriptRunner;
            _nugetRepo = nugetRepo;
            _scriptRepo = scriptRepo;
            _repoSettings = options.Value;
            _webSettings = webSettings.Value;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetScriptsAsync()
        {
            try
            {
                //todo - create a ScriptVM to remove some properties
                var packages = await _scriptRetriever.GetPackagesAsync();
                packages = packages.Where(w => IsAllowed(w.AllowedADGroups));

				return Json(new { data = packages });
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Unknown error");
                return Json(null);
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
        [DisableRequestSizeLimit]
        public async Task<IActionResult> RunScript(Package script)
        {
            try
            {
                //Play it safe and get the Script again :-)
                var repoScript = await _scriptRetriever.GetPackageOrDefaultAsync(script.UniqueId);
                repoScript = repoScript.CloneWithParams(script);

                if (IsAllowed(repoScript.AllowedADGroups))
                {
                    PackageResult result = null;
                    var currentUser = HttpContext.User.Identity.Name;

                    if (script.RunAsUser)
                    {
                        if (HttpContext.User.Identity is not WindowsIdentity windowsId)
                            throw new Exception("Authenticated user isn't a WindowsIdentity");

                        result = await WindowsIdentity.RunImpersonatedAsync(windowsId.AccessToken, async () => 
                                 await _scriptRunner.ExecuteAsync(repoScript, currentUser));
                    }
                    else result = await _scriptRunner.ExecuteAsync(repoScript, currentUser);

                    return Ok(result);
                }
                else
                {
                    return Unauthorized($"You are not allowed to execute the script {script.UniqueId}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error with {script.UniqueId}");
                return BadRequest(ex.Message);
            }
        }

		private bool IsAllowed(string[]? allowedGroupsAD)
		{
			var groups = HttpContext.User.Identity.Groups();

			if (allowedGroupsAD != null)
                _logger?.LogInformation($"IsAllowed {string.Join(";", allowedGroupsAD)} vs {string.Join(";", groups)}");
			
			if (allowedGroupsAD == null || groups.Where(w => allowedGroupsAD.Contains(w) == true).Any())
			{
                return true;
			}

            return false;
		}

		[HttpPost("/api/Script/ImportScript")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ImportScript(Package package)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(package.Category)) throw new Exception($"{nameof(package.Category)} must be populated");
                if (string.IsNullOrWhiteSpace(package.System)) throw new Exception($"{nameof(package.System)} must be populated");
                if (string.IsNullOrWhiteSpace(package.Title)) throw new Exception($"{nameof(package.Title)} must be populated");

                //Check for dups
                var repoScript = await _scriptRetriever.GetPackageOrDefaultAsync(package.UniqueId);
                if (repoScript != null)
                {
                    _logger.LogError($"UniqueId: {package.UniqueId} already exists");
                    throw new Exception($"Duplicate PackageId - {package.UniqueId}");
                }

                _logger?.LogInformation($"Importing {package.Id}");

                var filename = await _scriptRepo.ImportPackageAsync(package);
                return Ok(filename);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Unknown error Importing {package.UniqueId}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/Script/CreateScriptRunnerScript")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateScriptRunnerScript()
        {
            var filename = string.Empty;

            if(HttpContext.Request.Form.Files.Count != 1)
                throw new Exception($"Please specify a file");

            try
            {
                var file = HttpContext.Request.Form.Files[0];
                filename = file.FileName;

                string sql = string.Empty;

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    sql = await reader.ReadToEndAsync();
                }

                var json = string.Empty;

                sql = Regex.Replace(sql, "^\\/\\*((.|\\n)*?)\\*\\/", (d) =>
                {
                    json = d.Groups[1].Value;
                    return string.Empty;
                });

                sql = sql.Trim();

                var package = Newtonsoft.Json.JsonConvert.DeserializeObject<Package>(json);
                if (package == null || string.IsNullOrWhiteSpace(package.System))
                    throw new Exception($"The file is invalid - Failed to find Package json");

                //Do we have a nice Id?
                if(string.IsNullOrWhiteSpace(package.Id))
                {
                    //Convert the Title to an id
                    var id = package.Title;

                    //Remove unwanted chars
                    id = Regex.Replace(id, "[^a-zA-Z0-9- ]", "");                   

                    //TitleCase it
                    id = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id).Replace(" ", "");

                    package.Id = $"{package.System}-{id}";
                }

                //Check for dups
                var repoScript = await _scriptRetriever.GetPackageOrDefaultAsync(package.UniqueId);
                if(repoScript != null)
                {
                    _logger.LogError($"UniqueId: {package.UniqueId} already exists");
                    throw new Exception($"Duplicate PackageId - {package.UniqueId}");
                }

                var parameterisedSql = SqlScript.Parameterise(sql, package.Params);              

                var jObject = JObject.Parse(json);
                if (jObject.TryGetValue(nameof(SqlScript.ConnectionString), StringComparison.OrdinalIgnoreCase, out JToken? connToken))
                {
                    package.Scripts = new List<SqlScript> { new SqlScript { Filename = filename, Script = parameterisedSql, ConnectionString = connToken.ToString() } };
                    return Json(new { package, originalSql = sql });
                }
                else
                    throw new Exception($"Missing {nameof(SqlScript.ConnectionString)} property");
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Error importing {filename}");
                return BadRequest(ex.Message);
            }
        }
    }
}
