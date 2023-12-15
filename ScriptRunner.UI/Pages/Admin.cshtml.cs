using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using ScriptRunner.UI.Pages.Shared;
using System.Security.Claims;

namespace ScriptRunner.UI.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminModel : PageModel
    {
        private readonly IPackageRetriever _scriptRetriever;
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<AdminModel> _logger;

        public string NugetFolder => _repoSettings.NugetFolder;
        public string ScriptFolder => _repoSettings.ScriptFolder;
        public string GitRepo => _repoSettings.GitRepo;
        public string Tags => _repoSettings.Tag;

        public AdminModel(IPackageRetriever scriptRetriever, IOptions<RepoSettings> options, ILogger<AdminModel> logger)
        {
            _scriptRetriever = scriptRetriever;
            _repoSettings = options.Value;
            _logger = logger;
        }

        public void OnPostReload()
        {
            var currentUser = HttpContext.User.Identity.Name;
            _logger?.LogInformation($"{currentUser} - Refreshing InMemory Package Cache");
            
            _scriptRetriever.ClearPackageCache();
        }

        public async Task OnPostSyncAsync(string[] selectedIds)
        {
            var currentUser = HttpContext.User.Identity.Name;
            _logger?.LogInformation($"{currentUser} - Importing PackageIds: {string.Join(",", selectedIds)}");

            await _scriptRetriever.ImportPackagesAsync(currentUser, selectedIds);
        }
    }
}