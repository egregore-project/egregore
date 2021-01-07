// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Data;
using egregore.Extensions;
using egregore.Filters;
using egregore.Models;
using Lunr;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace egregore.Controllers
{
    [OntologyExists]
    public class DynamicController<T> : Controller where T : IRecord<T>, new()
    {
        private readonly T _example;
        private readonly ICacheRegion<SyndicationFeed> _feeds;
        private readonly IRecordStore _store;

        public DynamicController(IRecordStore store, ICacheRegion<SyndicationFeed> feeds)
        {
            _store = store;
            _feeds = feeds;
            _example = new T();

            ObjectValidator = new DynamicModelValidator();
        }

        private CancellationToken CancellationToken => HttpContext.RequestAborted;

        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string controller, [FromRoute] string ns, [FromRoute] ulong rs,
            [FromQuery(Name = "q")] string query = default)
        {
            var (records, total) = await QueryAsync(controller, ns, rs, query, CancellationToken);

            Response.Headers.Add(Constants.HeaderNames.XTotalCount, $"{total}");
            return Ok(records);
        }

        [HttpGet("api/{ns}/v{rs}/[controller]/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string controller, [FromRoute] string ns,
            [FromRoute] ulong rs, [FromRoute] Guid id)
        {
            var record = await _store.GetByIdAsync(id, CancellationToken);
            if (record == default)
                return NotFound();

            return Ok(_example.ToModel(record));
        }

        [HttpPost("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Post([FromRoute] string controller, [FromRoute] string ns,
            [FromRoute] ulong rs, [FromBody] T model)
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
        private async Task<(IEnumerable<T>, ulong)> QueryAsync(string type, string ns, ulong rs, string query = default, CancellationToken cancellationToken = default)
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

            var models = ProjectModelsFromRecords(records);

            return (models, total);
        }

        private IEnumerable<T> ProjectModelsFromRecords(IEnumerable<Record> records)
        {
            var models = new List<T>();
            foreach (var record in records)
            {
                var model = _example.ToModel(record);
                models.Add(model);
            }

            return models;
        }

        #region Syndication

        [AcceptCharset]
        [Accepts(Constants.MediaTypeNames.Application.RssXml, Constants.MediaTypeNames.Application.AtomXml, Constants.MediaTypeNames.Text.Xml)]
        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> GetSyndicationFeed([FromRoute] string controller, [FromHeader(Name = Constants.HeaderNames.Accept)] string contentType, [FromFilter] Encoding encoding, [FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
        {
            var mediaType = contentType?.ToLowerInvariant().Trim();
            var charset = encoding.WebName;
            var queryUrl = Request.GetEncodedUrl();

            var cacheKey = $"{mediaType}:{charset}:{queryUrl}";

            if (!Request.IsContentStale(cacheKey, _feeds, out var stream, out var lastModified, out var result))
                return result;

            if (stream != default || _feeds.TryGetValue(cacheKey, out stream))
                return ServeFeed(cacheKey, stream, mediaType, charset, lastModified);

            var (records, _) = await QueryAsync(controller, ns, rs, query, CancellationToken);
            if (!SyndicationGenerator.TryBuildFeedAsync(queryUrl, ns, rs, records, mediaType, encoding, out stream,
                out lastModified))
                return new UnsupportedMediaTypeResult();

            _feeds.Set(cacheKey, stream);
            _feeds.Set($"{cacheKey}:{HeaderNames.LastModified}", lastModified);

            return ServeFeed(cacheKey, stream, mediaType, charset, lastModified);
        }

        [NonAction]
        private IActionResult ServeFeed(string cacheKey, byte[] stream, string mediaType, string charset, DateTimeOffset? lastModified)
        {
            Response.Headers.TryAdd(HeaderNames.LastModified, $"{lastModified:R}");
            Response.AppendETags(_feeds, cacheKey, stream);
            return File(stream, $"{mediaType}; charset={charset}");
        }

        #endregion
    }
}