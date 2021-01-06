using System.Threading;
using egregore.Logging.LightningDb;
using Microsoft.AspNetCore.Mvc;

namespace egregore.Logging
{
    public class LoggingController : Controller
    {
        private readonly LightningLoggingStore _store;

        public LoggingController(LightningLoggingStore store)
        {
            _store = store;
        }

        [HttpGet("api/logs")]
        public IActionResult Index()
        {
            var entries = _store.Get(CancellationToken);

            return Ok(entries);
        }

        private CancellationToken CancellationToken => HttpContext.RequestAborted;
    }
}
