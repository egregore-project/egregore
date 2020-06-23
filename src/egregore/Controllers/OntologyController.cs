using egregore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    public class OntologyController : Controller
    {
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public OntologyController(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        [HttpGet("whois")]
        public IActionResult WhoIs()
        {
            return Ok(new
            {
                PublicKey = Crypto.ToHexString(_options.Value.PublicKey)
            });
        }
    }
}
