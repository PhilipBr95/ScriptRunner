using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Models.Packages;
using ScriptRunner.Library.Services;
using ScriptRunner.UI.Settings;
using System.Text.Json;

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

        //public async Task<IActionResult> GetHistoryAsync()
        //{
        //    var transactions = (await _historyService.GetActivitiesAsync<Package>())
        //                                             .OrderByDescending(o => o.CreatedDate)
        //                                             .Take(_webSettings.MaxHistoryItems);

        //    var json = JsonSerializer.Serialize<IEnumerable<Activity<Package>>>(transactions);

        //    return Json(json);
        //}

        public async Task<IActionResult> GetPackageAsync()
        {
            var transactions = (await _historyService.GetActivitiesAsync<Package>())
                                                     .OrderByDescending(o => o.CreatedDate)
                                                     .Take(_webSettings.MaxHistoryItems);
            return Json(transactions);
        }

        [Route("~/api/history/popular")]
        public async Task<IActionResult> GetPopularHistoryAsync()
        {
            var transactions = await _historyService.GetActivitiesAsync<Package>();
            var top5 = transactions.GroupBy(gb => gb.Data.Id)
                                   .OrderByDescending(o => o.Count())
                                   .Take(8)
                                   .Select(s => new { Id = s.Key, Package = s.First() })
                                   .Select(ss => new { ss.Id, ss.Package.System, Title = $"{ss.Package.Data.Category}\\{ss.Package.Data.Title}", Data = ss.Package.Data })
                                   ;

            return Json(top5);
        }
    }
}

