using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Ontology;
using Lunr;
using Microsoft.AspNetCore.Mvc;

namespace egregore.Controllers
{
    public class DynamicController<T> : Controller where T : IRecord<T>, new()
    {
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

        [HttpGet("api/{ns}/v{rs}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string ns, [FromRoute] ulong rs, [FromQuery(Name = "q")] string query = default)
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
                return NotFound();
            
            var models = new List<T>();
            foreach(var record in records)
                models.Add(_example.ToModel(record));

            Response.Headers.Add(Constants.HeaderNames.XTotalCount, $"{total}");

            return Ok(models);
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
    }
}