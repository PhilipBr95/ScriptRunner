using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Settings;

namespace ScriptRunner.UI.Pages
{
    public class DocsModel : PageModel
    {
        public string Tag { get; init; }
        public string ScriptFolder { get; init; }
        public string GitRepo { get; init; }

        public DocsModel(IOptions<RepoSettings> options)
        {
            Tag = options.Value.Tag;
            ScriptFolder = options.Value.ScriptFolder;
            GitRepo = options.Value.GitRepo;
        }
    }
}
