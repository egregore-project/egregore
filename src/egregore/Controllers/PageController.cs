using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Ontology;
using egregore.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    // FIXME: Add HTTP Caching

    public class PageController : Controller
    {
        private readonly IPageStore _store;

        // ReSharper disable once SuggestBaseTypeForParameter (controllers are scoped)
        public PageController(IPageStore store, IOptionsSnapshot<WebServerOptions> options)
        {
            _store = store;
            _store.Init(Path.Combine(Constants.DefaultRootPath, $"{options.Value.PublicKeyString}_pages.egg"));
        }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;
        
        [HttpGet("pages")]
        public async Task<IActionResult> GetPages()
        {
            var pages = await _store.GetAsync(CancellationToken);
            return Ok(pages);
        }

        
        [HttpGet("pages/{id}")]
        public async Task<IActionResult> GetPageById(Guid id)
        {
            var page = await _store.GetByIdAsync(id, CancellationToken);
            if (page == default)
                return NotFound();

            return Ok(page);
        }

        [HttpPost("pages")]
        public async Task<IActionResult> AddPage([FromBody] Page page)
        {
            // FIXME: add page slug

            await _store.AddPageAsync(page, CancellationToken);
            
            return Created($"/pages/{page.Uuid}", page);
        }
    }
}
