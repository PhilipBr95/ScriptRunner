using Microsoft.AspNetCore.Mvc;
using ScriptRunner.Library.Models;
using ScriptRunner.Library.Services;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IHistoryService historyService, ILogger<HistoryController> logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public async Task<IActionResult> GetHistoryAsync()
        {            
            var transactions = await _historyService.GetActivitiesAsync<Package>();
            return Json(transactions);
        }
    }
}

