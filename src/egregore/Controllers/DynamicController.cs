using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using egregore.Data;
using egregore.Filters;
using egregore.Ontology;
using Lunr;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using WyHash;

namespace egregore.Controllers
{
    public class DynamicController<T> : Controller where T : IRecord<T>, new()
    {
        public static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(SyndicationFeed)));
        
        private readonly IOntologyLog _ontology;
        private readonly IRecordStore _store;
        private readonly T _example;

        public DynamicController(IOntologyLog ontology, IRecordStore store)
        {
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

        // FIXME: strong cache, invalidated on listener
        [Accepts(Constants.MediaTypeNames.ApplicationRssXml, Constants.MediaTypeNames.ApplicationAtomXml, Constants.MediaTypeNames.TextXml)]
        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> GetSyndicationFeed([FromHeader(Name = Constants.HeaderNames.Accept)] string contentType, [FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var (records, total) = await QueryAsync(ns, rs, query);
            if (total == 0)
                return NotFound();

            var timestamp = TimeZoneLookup.Now.Timestamp;
            var id = WyHash64.ComputeHash64(Encoding.UTF8.GetBytes(Request.Path), Seed); // FIXME: need to normalize this to produce stable IDs

            var queryUri = new Uri(Request.GetEncodedUrl());
            var feed = new SyndicationFeed($"{typeof(T).Name} Query Feed", $"A feed containing {typeof(T).Name} records for the query specified by the feed URI'", queryUri, $"{id}", timestamp);

            var items = new List<SyndicationItem>();
            foreach(var record in records)
            {
                var title = $"{typeof(T).Name}";
                var description = $"Location of the {typeof(T).Name} record at the specified feed item URI";
                var uri = $"api/{ns}/v{rs}/{typeof(T).Name}/{record.Uuid}";
                var ts = timestamp; // FIXME: need to surface record creation timestamps

                var item = new SyndicationItem(title, description, new Uri(uri, UriKind.Relative), title, ts);
                items.Add(item);
            }

            feed.Items = items;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = false,
                Indent = true
            };

            SyndicationFeedFormatter formatter;

            var mediaType = contentType?.ToLowerInvariant().Trim();
            switch (mediaType)
            {
                case Constants.MediaTypeNames.ApplicationRssXml:
                case Constants.MediaTypeNames.TextXml:
                {
                    formatter = new Rss20FeedFormatter(feed, false);
                    break;
                }
                case Constants.MediaTypeNames.ApplicationAtomXml:
                {
                    formatter = new Atom10FeedFormatter(feed);
                    break;
                }
                default:
                    return new UnsupportedMediaTypeResult();
            }

            await using var ms = new MemoryStream();
            using var writer = XmlWriter.Create(ms, settings);
            formatter.WriteTo(writer);
            writer.Flush();

            return File(ms.ToArray(), $"{mediaType}; charset=utf-8");
        }

        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var (records, total) = await QueryAsync(ns, rs, query);
            if (total == 0)
                return NotFound();

            Response.Headers.Add(Constants.HeaderNames.XTotalCount, $"{total}");
            return Ok(records);
        }

        [HttpGet("api/{ns}/v{rs}/[controller]/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string ns, [FromRoute] ulong rs, [FromRoute] Guid id)
        {
            var record = await _store.GetByIdAsync(id);
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
            await _store.AddRecordAsync(record);

            return Created($"/api/{ns}/v{rs}/{controller?.ToLowerInvariant()}/{record.Uuid}", model);
        }

        private async Task<(IEnumerable<T>, ulong)> QueryAsync(string ns, ulong rs, string query = default)
        {
            IEnumerable<Record> records;
            ulong total;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var results = await _store.SearchAsync(query, HttpContext.RequestAborted).ToList();
                records = results;
                total = (ulong) results.Count;
            }
            else
            {
                records = await _store.GetByTypeAsync(typeof(T).Name, out total);
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