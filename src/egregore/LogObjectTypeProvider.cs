// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using egregore.Ontology;

namespace egregore
{
    internal sealed class LogObjectTypeProvider : ILogObjectTypeProvider
    {
        private readonly ConcurrentDictionary<ulong, Type> _index;
        private readonly ConcurrentDictionary<Type, ulong> _reverseIndex;
        private readonly ConcurrentDictionary<Type, ConstructorInfo> _serializers;

        public LogObjectTypeProvider()
        {
            _index = new ConcurrentDictionary<ulong, Type>();
            _reverseIndex = new ConcurrentDictionary<Type, ulong>();
            _serializers = new ConcurrentDictionary<Type, ConstructorInfo>();

            AddKnownType<Namespace>();
            AddKnownType<Schema>();
            AddKnownType<SchemaProperty>();
            AddKnownType<GrantRole>();
            AddKnownType<RevokeRole>();
        }

        public ulong? Get(Type type)
        {
            return !_reverseIndex.TryGetValue(type, out var result) ? (ulong?) null : result;
        }

        public Type Get(ulong typeId)
        {
            return !_index.TryGetValue(typeId, out var result) ? null : result;
        }

        public ILogSerialized Deserialize(Type type, LogDeserializeContext context)
        {
            if (!_serializers.TryGetValue(type, out var serializer))
                return null;
            var deserialized = serializer.Invoke(new object[] {context});
            return (ILogSerialized) deserialized;
        }

        private void AddKnownType<T>() where T : ILogSerialized
        {
            if (!TryAdd((ulong) _index.Count, typeof(T)))
                throw new TypeInitializationException(GetType().FullName,
                    new InvalidOperationException($"Unable to add {typeof(T).Name} to known types"));
        }

        private bool TryAdd(ulong id, Type type)
        {
            if (!_index.ContainsKey(id))
                return _index.TryAdd(id, type) &&
                       _reverseIndex.TryAdd(type, id) &&
                       _serializers.TryAdd(type, type.GetConstructor(new[] {typeof(LogDeserializeContext)}));

            return false;
        }
    }
}