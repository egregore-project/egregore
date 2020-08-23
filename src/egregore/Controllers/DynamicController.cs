using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Caching;
using egregore.Data;
using egregore.Extensions;
using egregore.Filters;
using egregore.Generators;
using egregore.Ontology;
using Lunr;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace egregore.Controllers
{
    [OntologyExists]
    public class DynamicController<T> : Controller where T : IRecord<T>, new()
    {
        private readonly ICacheRegion<SyndicationFeed> _cache;
        private readonly IOntologyLog _ontology;
        private readonly IRecordStore _store;
        private readonly T _example;
        
        private CancellationToken CancellationToken => HttpContext.RequestAborted;

        public DynamicController(ICacheRegion<SyndicationFeed> cache, IOntologyLog ontology, IRecordStore store)
        {
            _cache = cache;
            _ontology = ontology;
            _store = store;
            _example = new T();
        }

        [HttpOptions("api/{ns}/v{rs}/[controller]")]
        public IActionResult Options([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs)
        {
            var schemas = _ontology.GetSchemas(ns, rs);
            return Ok(schemas);
        }

        [AcceptCharset]
        [Accepts(Constants.MediaTypeNames.ApplicationRssXml, Constants.MediaTypeNames.ApplicationAtomXml, Constants.MediaTypeNames.TextXml)]
        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> GetSyndicationFeed([FromRoute] string controller, [FromHeader(Name = Constants.HeaderNames.Accept)] string contentType, [FromFilter] Encoding encoding, [FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var mediaType = contentType?.ToLowerInvariant().Trim();
            var charset = encoding.WebName;
            var queryUrl = Request.GetEncodedUrl();
            
            var cacheKey = $"{mediaType}:{charset}:{queryUrl}";

            if (!Request.IfNoneMatch(cacheKey, _cache, out var stream, out var result))
                return result;

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Modified-Since
            // "When used in combination with If-None-Match, it is ignored, unless the server doesn't support If-None-Match."
            if (!Request.IfModifiedSince(cacheKey, _cache, out var lastModified, out result))
                return result;

            if (stream != default || _cache.TryGetValue(cacheKey, out stream))
            {
                Response.Headers.TryAdd(HeaderNames.LastModified, $"{lastModified:R}");
                Response.AppendETags(cacheKey, stream);
                return File(stream, $"{mediaType}; charset={charset}");
            }

            var (records, _) = await QueryAsync(ns, rs, query, CancellationToken);
            if (!SyndicationGenerator.TryBuildFeedAsync(queryUrl, ns, rs, records, mediaType, encoding, out stream, out lastModified))
                return UnsupportedMediaType();
            _cache.Set(cacheKey, stream);
            _cache.Set($"{cacheKey}:{HeaderNames.LastModified}", lastModified);
            
            Response.Headers.TryAdd(HeaderNames.LastModified, $"{lastModified:R}");
            Response.AppendETags(cacheKey, stream);

            return File(stream, $"{mediaType}; charset={charset}");
        }

        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var (records, total) = await QueryAsync(ns, rs, query, CancellationToken);
            
            Response.Headers.Add(Constants.HeaderNames.XTotalCount, $"{total}");
            return Ok(records);
        }

        [HttpGet("api/{ns}/v{rs}/[controller]/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs, [FromRoute] Guid id)
        {
            var record = await _store.GetByIdAsync(id, CancellationToken);
            if (record == default)
                return NotFound();

            return Ok(_example.ToModel(record));
        }

        [HttpPost("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Post([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs, [FromBody] T model)
        {
            if (model.Uuid != default)
                return BadRequest();

            if (!TryValidateModel(model))
                return BadRequest();

            model.Uuid = Guid.NewGuid();

            var record = model.ToRecord();
            await _store.AddRecordAsync(record, cancellationToken: CancellationToken);

            return Created($"/api/{ns}/v{rs}/{controller?.ToLowerInvariant()}/{record.Uuid}", model);
        }
        
        [NonAction]
        private async Task<(IEnumerable<T>, ulong)> QueryAsync(string ns, ulong rs, string query = default, CancellationToken cancellationToken = default)
        {
            IEnumerable<Record> records;
            ulong total;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var results = await _store.SearchAsync(query, cancellationToken).ToList();
                records = results;
                total = (ulong) results.Count;
            }
            else
            {
                records = await _store.GetByTypeAsync(typeof(T).Name, out total, cancellationToken);
            }

            if (records == null)
                return (Enumerable.Empty<T>(), 0);
            
            var models = new List<T>();
            foreach(var record in records)
                models.Add(_example.ToModel(record));

            return (models, total);
        }
        
        [NonAction]
        private static UnsupportedMediaTypeResult UnsupportedMediaType()
        {
            return new UnsupportedMediaTypeResult();
        }
    }
}