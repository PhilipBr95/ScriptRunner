using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using ScriptRunner.UI.Pages.Shared;

namespace ScriptRunner.UI.Pages
{
    public class AdminModel : PageModel
    {
        private readonly IPackageRetriever _scriptRetriever;
        private readonly RepoSettings _repoSettings;
        private readonly ILogger<AdminModel> _logger;

        public string NugetFolder => _repoSettings.NugetFolder;
        public string ScriptFolder => _repoSettings.ScriptFolder;
        public string GitRepo => _repoSettings.GitRepo;
        public string Tags => _repoSettings.Tags;

        public AdminModel(IPackageRetriever scriptRetriever, IOptions<RepoSettings> options, ILogger<AdminModel> logger)
        {
            _scriptRetriever = scriptRetriever;
            _repoSettings = options.Value;
            _logger = logger;
        }

        public void OnPostReload()
        {
            _scriptRetriever.ClearPackageCache();
        }
        public async Task OnPostSyncAsync()
        {
            await _scriptRetriever.ImportPackagesAsync();
        }
    }
}