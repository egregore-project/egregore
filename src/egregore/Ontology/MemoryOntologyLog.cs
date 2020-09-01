// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using egregore.Events;
using egregore.Generators;
using egregore.Hubs;
using egregore.Ontology.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Ontology
{
    public sealed class MemoryOntologyLog : IOntologyLog
    {
        private readonly OntologyEvents _events;
        private static readonly Namespace Default = new Namespace(Constants.DefaultNamespace);

        private long _index;
        private Namespace _namespace;

        public long Index => Interlocked.Read(ref _index);

        public void Init(ReadOnlySpan<byte> publicKey)
        {
            Namespaces = new List<Namespace> {Default};
            _namespace = Namespaces[0];

            Manifest = new Dictionary<string, Dictionary<ulong, List<Schema>>>(StringComparer.OrdinalIgnoreCase);
            Manifest.TryAdd(Constants.DefaultNamespace, new Dictionary<ulong, List<Schema>>());

            Schemas = new Dictionary<string, Dictionary<ulong, List<Schema>>>(StringComparer.OrdinalIgnoreCase);
            Schemas.TryAdd(Constants.DefaultNamespace, new Dictionary<ulong, List<Schema>>());

            Revisions = new Dictionary<string, Dictionary<string, ulong>>(StringComparer.OrdinalIgnoreCase);
            Revisions.TryAdd(Constants.DefaultNamespace, new Dictionary<string, ulong>());

            Roles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Roles.TryAdd(Constants.DefaultNamespace, new List<string> {Constants.DefaultOwnerRole});

            RoleGrants = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
            RoleGrants.TryAdd(Constants.DefaultNamespace, new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {Constants.DefaultOwnerRole, new List<string> {publicKey.ToHexString()}}
            });
        }

        public Dictionary<string, Dictionary<ulong, List<Schema>>> Manifest { get; set; }

        // ReSharper disable once UnusedMember.Global (used for DI)
        public MemoryOntologyLog(OntologyEvents events)
        {
            _events = events;
        }

        internal MemoryOntologyLog(OntologyEvents events, ReadOnlySpan<byte> publicKey)
        {
            _events = events;
            Init(publicKey);
        }
        
        public List<Namespace> Namespaces { get; set; }
        public Dictionary<string, Dictionary<ulong, List<Schema>>> Schemas { get; set; }
        public Dictionary<string, Dictionary<string, ulong>> Revisions { get; set; }
        public Dictionary<string, List<string>> Roles { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> RoleGrants { get; set; }

        public async Task MaterializeAsync(ILogStore store, byte[] secretKey = default, long? startingFrom = default, CancellationToken cancellationToken = default)
        {
            if (startingFrom == default)
                startingFrom = Interlocked.Read(ref _index) + 1;

            foreach (var entry in store.StreamEntries((ulong) startingFrom, secretKey))
            {
                Interlocked.Exchange(ref _index, (long) entry.Index.GetValueOrDefault());

                foreach (var @object in entry.Objects)
                    switch (@object.Data)
                    {
                        case Namespace ns:
                        {
                            var key = ns;
                            Namespaces.Add(key);
                            _namespace = key;

                            if (!Revisions.ContainsKey(_namespace.Value))
                                Revisions.Add(_namespace.Value, new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase));

                            if (!Manifest.ContainsKey(_namespace.Value))
                                Manifest.Add(_namespace.Value, new Dictionary<ulong, List<Schema>>());

                            if (!Roles.ContainsKey(_namespace.Value))
                                Roles.Add(_namespace.Value, new List<string>());

                            break;
                        }
                        case Schema schema:
                        {
                            var key = schema.Name;

                            if (!Revisions[_namespace.Value].TryGetValue(key, out var revision))
                                Revisions[_namespace.Value].Add(key, revision = 1);

                            if (!Manifest[_namespace.Value].TryGetValue(revision, out var manifest))
                                Manifest[_namespace.Value].Add(revision, manifest = new List<Schema>());

                            if (!Schemas.TryGetValue(key, out var schemaMap))
                                Schemas.Add(key, schemaMap = new Dictionary<ulong, List<Schema>>());

                            if (!schemaMap.TryGetValue(revision, out var list))
                                schemaMap.Add(revision, list = new List<Schema>());

                            manifest.Add(schema);
                            list.Add(schema);

                            await _events.OnSchemaAddedAsync(store, schema, cancellationToken);
                            break;
                        }
                        case RevokeRole revokeRole:
                        {
                            if (!revokeRole.Verify())
                                throw new InvalidOperationException($"invalid {revokeRole.Type}");

                            if (RoleGrants.TryGetValue(_namespace.Value, out var lookup) &&
                                lookup.Count == 1 &&
                                lookup[Constants.DefaultOwnerRole].Count == 1)
                                throw new CannotRemoveSingleOwnerException("cannot revoke admin rights of only owner");

                            break;
                        }
                        default:
                            throw new NotImplementedException(@object.Data.GetType().Name);
                    }
            }
        }

        public bool Exists(string eggPath)
        {
            return Directory.Exists(eggPath);
        }

        public string GenerateModels()
        {
            var sb = new IndentAwareStringBuilder();
            var generator = new ModelGenerator();
            foreach (var manifest in Schemas)
            {
                var ns = new Namespace(manifest.Key);

                foreach (var revisionSet in manifest.Value)
                {
                    var revision = revisionSet.Key;

                    foreach(var schema in revisionSet.Value)
                    {
                        generator.Generate(sb, ns, revision, schema);
                    }
                }
            }

            sb.InsertAutoGeneratedHeader();
            var code = sb.ToString();
            return code;
        } 

        public IEnumerable<Schema> GetSchemas(string ns, ulong revision)
        {
            if (Manifest.TryGetValue(ns, out var map))
            {
                if (map.TryGetValue(revision, out var schemas))
                {
                    return schemas;
                }
            }

            return Enumerable.Empty<Schema>();
        }

        public Schema GetSchema(string name, string ns, ulong revision)
        {
            if (!Manifest.TryGetValue(ns, out var map) || !map.TryGetValue(revision, out var schemas))
                return default;
            return schemas.FirstOrDefault(schema => schema.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}