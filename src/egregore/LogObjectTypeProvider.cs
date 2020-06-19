// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using egregore.Schema;

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

            if(!TryAdd(1, typeof(Namespace)))
                throw new TypeInitializationException(GetType().FullName, new InvalidOperationException(
                    $"Unable to add {nameof(Namespace)} to known types"));
        }

        private bool TryAdd(ulong id, Type type)
        {
            if (!_index.ContainsKey(id))
            {
                return _index.TryAdd(id, type) &&
                       _reverseIndex.TryAdd(type, id) &&
                       _serializers.TryAdd(type, type.GetConstructor(new[] {typeof(LogDeserializeContext)}));
            }

            return false;
        }

        public ulong? Get(Type type) => !_reverseIndex.TryGetValue(type, out var result) ? (ulong?) null : result;

        public Type Get(ulong typeId) => !_index.TryGetValue(typeId, out var result) ? null : result;

        public ILogSerialized Deserialize(Type type, LogDeserializeContext context)
        {
            if (!_serializers.TryGetValue(type, out var serializer))
                return null;
            var deserialized = serializer.Invoke(new object[] { context });
            return (ILogSerialized) deserialized;
        }
    }
}