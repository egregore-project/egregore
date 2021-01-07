// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Globalization;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Models;
using egregore.Ontology;
using Microsoft.AspNetCore.Mvc;

namespace egregore.Controllers
{
    public class OntologyController : Controller
    {
        private readonly IOntologyLog _ontology;
        private readonly OntologyWriter _writer;

        public OntologyController(IOntologyLog ontology, OntologyWriter writer)
        {
            _ontology = ontology;
            _writer = writer;
        }

        [HttpOptions("api/{ns}/v{rs}")]
        public IActionResult Options([FromRoute] string ns, [FromRoute] ulong rs)
        {
            var schemas = _ontology.GetSchemas(ns, rs);
            return Ok(schemas);
        }

        [HttpPost("api/{ns}/{controller}")]
        public async Task<IActionResult> Post([FromRoute] string controller, [FromRoute] string ns, [FromBody] object model)
        {
            await SaveSchemaAsync(controller, model);

            return RedirectToActionPermanent(nameof(Post), "Dynamic", new { controller, ns, model });
        }

        [HttpGet("ontology")]
        public IActionResult GetOntology()
        {
            var schemas = _ontology.GetSchemas("default");
            return Ok(new OntologyViewModel {Schemas = schemas});
        }

        [HttpPost("schema")]
        public async Task<IActionResult> AddSchema()
        {
            var schema = new Schema {Name = "Customer"};
            var property = new SchemaProperty {Name = "Name", Type = "string", IsRequired = true};
            schema.Properties.Add(property);

            await _writer.SaveAsync(schema);
            return Ok(schema);
        }

        private async Task SaveSchemaAsync(string controller, object model)
        {
            var schema = new Schema
            {
                Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(controller)
            };
            var type = model.GetType();
            foreach (var p in type.GetProperties())
            {
                var property = new SchemaProperty {Type = p.PropertyType.Name, Name = p.Name, IsRequired = false};
                schema.Properties.Add(property);
            }

            await _writer.SaveAsync(schema);
        }
    }
}