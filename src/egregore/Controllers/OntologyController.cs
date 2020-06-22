using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    public class OntologyController : Controller
    {
        private readonly IOptionsSnapshot<ServerOptions> _options;

        public OntologyController(IOptionsSnapshot<ServerOptions> options)
        {
            _options = options;
        }

        [HttpGet("whois")]
        public IActionResult WhoIs()
        {
            return Ok(new
            {
                PublicKey = Crypto.HexString(_options.Value.PublicKey)
            });
        }
    }
}
