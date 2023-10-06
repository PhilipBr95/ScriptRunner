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

        public class PackageModel
        {
            public string System { get; set; }
            public string Title{ get; set; }

            public string Id { get; set; }
            public string Version { get; set; }
            public DateTime? ImportedDate { get; set; }

            //public PackageModel(IPackageSearchMetadata s)
            //{
            //    Version = s.Identity.Version.OriginalVersion;
            //    Title = s.Title;
            //    Id = s.Identity.Id;
            //}

			//public PackageModel(Package s)
			//{
            //    System = s.System;
            //    Title = s.titl
			//}
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
                var repoScript = await _scriptRetriever.GetPackageAsync(script.Id, script.Version);
                repoScript = repoScript.CloneWithParams(script);
                
                if (IsAllowed(repoScript.AllowedADGroups))
                {
					var currentUser = HttpContext.User.Identity.Name;
					var result = await _scriptRunner.ExecuteAsync(repoScript, currentUser);

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

            try
            {
                var file = HttpContext.Request.Form.Files.FirstOrDefault();
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
