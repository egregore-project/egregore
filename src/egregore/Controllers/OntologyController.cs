// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Ontology;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    public class OntologyController : Controller
    {
        private readonly ILogStore _store;
        private readonly IOntologyLog _ontology;
        private readonly WebServerHostedService _service;
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public OntologyController(ILogStore store, IOntologyLog ontology, WebServerHostedService service, IOptionsSnapshot<WebServerOptions> options)
        {
            _store = store;
            _ontology = ontology;
            _service = service;
            _options = options;
        }
        
        [HttpPost("schema")]
        public async Task<IActionResult> AddSchema()
        {
            _store.Init(_options.Value.EggPath);

            var schema = new Schema {Name = "Customer"};
            schema.Properties.Add(new SchemaProperty {Name = "Name", Type = "string"});

            var typeProvider = new LogObjectTypeProvider();
            var hashProvider = new LogEntryHashProvider(typeProvider);

            var type = typeProvider.Get(schema.GetType()).GetValueOrDefault();
            var version = LogSerializeContext.FormatVersion;

            var @object = new LogObject
            {
                Timestamp = TimestampFactory.Now,
                Type = type,
                Version = version,
                Data = schema
            };

            @object.Hash = hashProvider.ComputeHashBytes(@object);

            var previousHash = new byte[0];
            foreach(var item in _store.StreamEntries())
                previousHash = item.Hash;
            
            var entry = new LogEntry
            {
                PreviousHash = previousHash,
                Timestamp = @object.Timestamp,
                Nonce = Crypto.Nonce(64U),
                Objects = new[] {@object}
            };

            entry.HashRoot = hashProvider.ComputeHashRootBytes(entry);
            entry.Hash = hashProvider.ComputeHashBytes(entry);

            var index = await _store.AddEntryAsync(entry);
            _service.OnOntologyChanged((long) index);
            return Ok(schema);
        }
    }
}