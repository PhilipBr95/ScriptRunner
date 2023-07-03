using Microsoft.AspNetCore.Mvc;
using ScriptRunner.Library.Services;
using System.Transactions;

namespace ScriptRunner.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : Controller
    {
        private readonly IMemoryLoggerProvider _memoryLoggerProvider;

        public LogController(IMemoryLoggerProvider memoryLoggerProvider) 
        {
            _memoryLoggerProvider = memoryLoggerProvider;
        }
        
        public async Task<IActionResult> GetLogsAsync()
        {            
            return await Task.FromResult(Json(_memoryLoggerProvider.MemoryLogs));
        }
    }
}
