// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace egregore.Data
{
    public sealed class LogObjectTypeProvider : ILogObjectTypeProvider, IEquatable<LogObjectTypeProvider>
	{
		private readonly ConcurrentDictionary<ulong, Type> _index;
		private readonly ConcurrentDictionary<Type, ulong> _reverseIndex;
		private readonly ConcurrentDictionary<Type, ConstructorInfo> _serializers;

		public LogObjectTypeProvider()
		{
			_index = new ConcurrentDictionary<ulong, Type>();
			_reverseIndex = new ConcurrentDictionary<Type, ulong>();
			_serializers = new ConcurrentDictionary<Type, ConstructorInfo>();
		}

		public ulong? Get(Type type)
		{
			return !_reverseIndex.TryGetValue(type, out var result) ? AddKnownType(type) as ulong? : result;
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

		public ulong AddKnownType(Type type)
		{
			var next = (ulong) _index.Count;
			
			if (!TryAdd(next, type))
				throw new TypeInitializationException(GetType().FullName,
					new InvalidOperationException($"Unable to add {type.Name} to known types"));

			return next;
		}

		private bool TryAdd(ulong id, Type type)
		{
			if (!_index.ContainsKey(id))
				return _index.TryAdd(id, type) &&
				       _reverseIndex.TryAdd(type, id) &&
				       _serializers.TryAdd(type, type.GetConstructor(new[] {typeof(LogDeserializeContext)}));

			return false;
		}

		#region Equality Members

		public bool Equals(LogObjectTypeProvider other) => other is not null && (ReferenceEquals(this, other) || Equals(_index, other._index));
		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is LogObjectTypeProvider other && Equals(other);
		public static bool operator ==(LogObjectTypeProvider left, LogObjectTypeProvider right) => Equals(left, right);
		public static bool operator !=(LogObjectTypeProvider left, LogObjectTypeProvider right) => !Equals(left, right);

		public override int GetHashCode()
		{
			var hash = default(int);

			foreach (var (key, value) in _index)
			{
				var keyParts = BitConverter.GetBytes(key);

				hash ^= keyParts[0];
				hash ^= keyParts[1];
				hash ^= keyParts[2];
				hash ^= keyParts[3];
				hash ^= keyParts[4];
				hash ^= keyParts[5];
				hash ^= keyParts[6];
				hash ^= keyParts[7];

				hash ^= value.GetHashCode();
			}

			return hash;
		}

		#endregion
	}
}