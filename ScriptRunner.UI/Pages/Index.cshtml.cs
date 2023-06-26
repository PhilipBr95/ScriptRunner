using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScriptRunner.Library;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Repos;

namespace ScriptRunner.UI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly INugetRepo _scriptRepo;
        private readonly ILogger<IndexModel> _logger;
        public Package SelectedScript { get; set; }

        public IEnumerable<Package> Scripts { get; private set; }
        public string Systems { get; private set; }

        public IndexModel(INugetRepo scriptRepo, ILogger<IndexModel> logger)
        {
            _scriptRepo = scriptRepo;
            _logger = logger;

        }

        public async Task OnGetAsync()
        {
            Scripts = await _scriptRepo.GetScriptsAsync();
        }
    }
}