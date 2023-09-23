using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Services;
using ScriptRunner.UI.Settings;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly WebSettings _webSettings;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IHistoryService historyService, IOptions<WebSettings> options, ILogger<HistoryController> logger)
        {
            _historyService = historyService;
            _webSettings = options.Value;
            _logger = logger;
        }

        public async Task<IActionResult> GetHistoryAsync()
        {            
            var transactions = (await _historyService.GetActivitiesAsync<Package>())
                                                     .OrderByDescending(o => o.CreatedDate)
                                                     .Take(_webSettings.MaxHistoryItems);
            return Json(transactions);
        }
    }
}

