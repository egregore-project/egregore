// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Media;
using egregore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace egregore.Controllers
{
    public class MediaController : Controller
    {
        private readonly IMediaStore _store;

        // ReSharper disable once SuggestBaseTypeForParameter (controllers are scoped)
        public MediaController(IMediaStore store, IOptionsSnapshot<WebServerOptions> options)
        {
            _store = store;
            _store.Init(Path.Combine(Constants.DefaultRootPath, $"{options.Value.PublicKeyString}_media.egg"));
        }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        [HttpGet("media")]
        public async Task<IActionResult> Get()
        {
            var entries = await _store.GetAsync(CancellationToken);

            // FIXME: directly stream the headers to avoid streaming the whole payload as well
            return Ok(entries.Select(x => new MediaEntryViewModel
            {
                Uuid = x.Uuid,
                Type = x.Type,
                Length = x.Length,
                Name = x.Name
            }));
        }

        [HttpGet("media/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entry = await _store.GetByIdAsync(id, CancellationToken);
            if (entry.Length == 0)
                return NotFound();

            return File(entry.Data, entry.Type);
        }

        [HttpPost("media")]
        public async Task<IActionResult> Upload()
        {
            ContentDispositionHeaderValue disposition;
            if (Request.Headers.TryGetValue(Constants.HeaderNames.ContentDisposition, out var dispositionHeader))
                ContentDispositionHeaderValue.TryParse(dispositionHeader.ToString(), out disposition);
            else
                return BadRequest();

            HttpContext.Request.Headers.TryGetValue(Constants.HeaderNames.ContentType, out var mediaType);
            var mediaTypeName = mediaType.ToString();

            return mediaTypeName switch
            {
                "image/gif" => await UploadImageAsync(disposition),
                "image/tiff" => await UploadImageAsync(disposition),
                "image/jpeg" => await UploadImageAsync(disposition),
                "image/Png" => await UploadImageAsync(disposition),

                _ => new UnsupportedMediaTypeResult()
            };
        }

        private async Task<IActionResult> UploadImageAsync(ContentDispositionHeaderValue disposition)
        {
            StringValues mediaType;
            await using var body = new MemoryStream();
            await HttpContext.Request.Body.CopyToAsync(body, CancellationToken);
            body.Position = 0;

            using var image = Image.Load(body);
            //image.Mutate(x => x.Resize(256, 256));
            //image.Save("...");

            await using var ms = new MemoryStream();

            var encoder = new PngEncoder
            {
                IgnoreMetadata = true
            };

            await image.SaveAsPngAsync(ms, encoder, CancellationToken);
            mediaType = Constants.MediaTypeNames.Image.Png;

            var buffer = ms.ToArray();
            var media = new MediaEntry
            {
                Type = mediaType,
                Name = disposition.FileName.Value,
                Data = buffer,
                Length = (ulong) buffer.Length
            };

            await _store.AddMediaAsync(media, CancellationToken);

            var model = new MediaEntryViewModel
            {
                Type = media.Type,
                Name = media.Name,
                Uuid = media.Uuid,
                Length = media.Length
            };

            return Created($"/media/{media.Uuid}", model);
        }
    }
}