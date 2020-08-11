// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using egregore.Hubs;
using egregore.Ontology.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Ontology
{
    public sealed class MemoryOntologyLog : IOntologyLog
    {
        private static readonly Namespace Default = new Namespace(Constants.DefaultNamespace);

        private long _index;
        private Namespace _namespace;

        public long Index => Interlocked.Read(ref _index);

        public void Init(ReadOnlySpan<byte> publicKey)
        {
            Namespaces = new List<Namespace> {Default};
            _namespace = Namespaces[0];

            Schemas = new Dictionary<string, Dictionary<ulong, List<Schema>>>();
            Schemas.TryAdd(Constants.DefaultNamespace, new Dictionary<ulong, List<Schema>>());

            Revisions = new Dictionary<string, Dictionary<string, ulong>>();
            Revisions.TryAdd(Constants.DefaultNamespace, new Dictionary<string, ulong>());

            Roles = new Dictionary<string, List<string>>();
            Roles.TryAdd(Constants.DefaultNamespace, new List<string> {Constants.DefaultOwnerRole});

            RoleGrants = new Dictionary<string, Dictionary<string, List<string>>>();
            RoleGrants.TryAdd(Constants.DefaultNamespace, new Dictionary<string, List<string>>
            {
                {Constants.DefaultOwnerRole, new List<string> {publicKey.ToHexString()}}
            });
        }

        // ReSharper disable once UnusedMember.Global (used for DI)
        public MemoryOntologyLog() { }

        internal MemoryOntologyLog(ReadOnlySpan<byte> publicKey) => Init(publicKey);
        internal MemoryOntologyLog(ReadOnlySpan<byte> publicKey, ILogStore store, ulong startingFrom = 0UL, byte[] secretKey = null) : this(publicKey)
        {
            Interlocked.Exchange(ref _index, (long) startingFrom);
            Materialize(store, default, default, secretKey);
        }

        public List<Namespace> Namespaces { get; set; }
        public Dictionary<string, Dictionary<ulong, List<Schema>>> Schemas { get; set; }
        public Dictionary<string, Dictionary<string, ulong>> Revisions { get; set; }
        public Dictionary<string, List<string>> Roles { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> RoleGrants { get; set; }

        public void Materialize(ILogStore store, IHubContext<NotificationHub> hub = default,
            OntologyChangeProvider change = default, byte[] secretKey = default)
        {
            var startingFrom = Interlocked.Read(ref _index);

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
                                Revisions.Add(_namespace.Value, new Dictionary<string, ulong>());

                            if (!Roles.ContainsKey(_namespace.Value))
                                Roles.Add(_namespace.Value, new List<string>());

                            break;
                        }
                        case Schema schema:
                        {
                            var key = schema.Name;

                            if (!Revisions[_namespace.Value].TryGetValue(key, out var revision))
                                Revisions[_namespace.Value].Add(key, revision = 1);

                            if (!Schemas.TryGetValue(key, out var schemaMap))
                                Schemas.Add(key, schemaMap = new Dictionary<ulong, List<Schema>>());

                            if (!schemaMap.TryGetValue(revision, out var list))
                                schemaMap.Add(revision, list = new List<Schema>());

                            list.Add(schema);
                            OnSchemaAdded(schema, hub, change);
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

        private static void OnSchemaAdded(Schema schema, IHubContext<NotificationHub> hub,
            OntologyChangeProvider change)
        {
            hub?.Clients.All.SendAsync("ReceiveMessage", "info", $"Adding new schema '{schema.Name}'");
            change?.OnChanged();
        }
    }
}