using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Caching;
using egregore.Data;
using egregore.Filters;
using egregore.Ontology;
using Lunr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using WyHash;

namespace egregore.Controllers
{
    internal static class ETagExtensions
    {
        private static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(ETagExtensions)));

        public static string StrongETag(this byte[] data)
        {
            var value = WyHash64.ComputeHash64(data, Seed);
            return $"\"{value}\"";
        }

        public static string WeakETag(this string key, bool prefix)
        {
            var value = WyHash64.ComputeHash64(Encoding.UTF8.GetBytes(key), Seed);
            return prefix ? $"W/\"{value}\"" : $"\"{value}\"";
        }
    }

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
        public IActionResult Options([FromRoute] string ns, [FromRoute] ulong rs)
        {
            var schemas = _ontology.GetSchemas(ns, rs);
            return Ok(schemas);
        }

        // FIXME: client cache with E-Tags
        [AcceptCharset]
        [Accepts(Constants.MediaTypeNames.ApplicationRssXml, Constants.MediaTypeNames.ApplicationAtomXml, Constants.MediaTypeNames.TextXml)]
        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> GetSyndicationFeed([FromHeader(Name = Constants.HeaderNames.Accept)] string contentType, [FromFilter] Encoding encoding, [FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var mediaType = contentType?.ToLowerInvariant().Trim();
            var queryUrl = Request.GetEncodedUrl();
            var charset = encoding.WebName;

            var cacheKey = $"{mediaType}:{charset}:{queryUrl}";

            var headers = Request.GetTypedHeaders();

            byte[] stream = default;

            foreach (var etag in headers.IfNoneMatch)
            {
                if (etag.IsWeak)
                {
                    if (etag.Tag.Equals(cacheKey.WeakETag(false)))
                    {
                        return NotModified();
                    }
                }
                else
                {
                    if (_cache.TryGetValue(cacheKey, out stream) && etag.Tag.Equals(stream.StrongETag()))
                    {
                        return NotModified();
                    }
                }
            }

            if (stream != default || _cache.TryGetValue(cacheKey, out stream))
            {
                Response.Headers.TryAdd(HeaderNames.ETag, new StringValues(new[] { cacheKey.WeakETag(true), stream.StrongETag() }));
                return File(stream, $"{mediaType}; charset={charset}");
            }

            var (records, total) = await QueryAsync(ns, rs, query, CancellationToken);
            if (total == 0)
                return NotFound();

            if (!FeedBuilder.TryBuildFeedAsync(queryUrl, ns, rs, records, mediaType, encoding, out stream))
                return new UnsupportedMediaTypeResult();

            _cache.Set(cacheKey, stream);

            Response.Headers.TryAdd(HeaderNames.ETag, new StringValues(new[] { cacheKey.WeakETag(true), stream.StrongETag() }));
            return File(stream, $"{mediaType}; charset={charset}");
        }

        private StatusCodeResult NotModified()
        {
            return StatusCode((int) HttpStatusCode.NotModified);
        }

        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var (records, total) = await QueryAsync(ns, rs, query, CancellationToken);
            if (total == 0)
                return NotFound();

            Response.Headers.Add(Constants.HeaderNames.XTotalCount, $"{total}");
            return Ok(records);
        }

        [HttpGet("api/{ns}/v{rs}/[controller]/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string ns, [FromRoute] ulong rs, [FromRoute] Guid id)
        {
            var record = await _store.GetByIdAsync(id, CancellationToken);
            if (record == default)
                return NotFound();

            return Ok(_example.ToModel(record));
        }

        [HttpPost("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Post([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs, [FromBody] T model)
        {
            var schema = _ontology.GetSchema(controller, ns, rs);
            if (schema == default)
                return NotFound();

            if (model.Uuid != default)
                return BadRequest();

            if (!TryValidateModel(model))
                return BadRequest();

            model.Uuid = Guid.NewGuid();

            var record = model.ToRecord();
            await _store.AddRecordAsync(record, cancellationToken: HttpContext.RequestAborted);

            return Created($"/api/{ns}/v{rs}/{controller?.ToLowerInvariant()}/{record.Uuid}", model);
        }

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
    }
}