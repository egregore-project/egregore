// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Media;
using egregore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace egregore.Controllers
{
    public class MediaController : Controller
    {
        private readonly IMediaStore _store;

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
            return Ok(entries);
        }


        [HttpGet("media/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var buffer = (await _store.GetByIdAsync(id, CancellationToken)) ?? new byte[0];
            if (buffer.Length == 0)
                return NotFound();

            return File(buffer, "image/png");
        }

        [HttpPost("media")]
        public async Task<IActionResult> Upload()
        {
            await using var body = new MemoryStream();
            await HttpContext.Request.Body.CopyToAsync(body, CancellationToken);
            body.Position = 0;

            HttpContext.Request.Headers.TryGetValue("Content-Type", out var mediaType);
            
            using var image = Image.Load(body);
            //image.Mutate(x => x.Resize(256, 256));
            //image.Save("...");

            await using var ms = new MemoryStream();
            var encoder = new PngEncoder();
            await image.SaveAsPngAsync(ms, encoder, CancellationToken);

            var media = new MediaEntry {Data = ms.ToArray()};
            await _store.AddMediaAsync(media, CancellationToken);

            var model = new MediaEntryViewModel
            {
                Uuid = media.Uuid,
                Type = mediaType
            };

            return Created($"/media/{media.Uuid}", model);
        }
    }
}