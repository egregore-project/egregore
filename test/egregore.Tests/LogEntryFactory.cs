// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Ontology;

namespace egregore.Tests
{
    internal static class LogEntryFactory
    {
        public static readonly LogEntryHashProvider HashProvider;
        public static readonly LogObjectTypeProvider TypeProvider;

        static LogEntryFactory()
        {
            TypeProvider = new LogObjectTypeProvider();
            HashProvider = new LogEntryHashProvider(TypeProvider);
        }

        public static LogEntry CreateNamespaceEntry(string value, byte[] previousHash)
        {
            return WrapObject(TypeProvider.Get(typeof(Namespace)).GetValueOrDefault(),
                LogSerializeContext.FormatVersion, new Namespace(value), previousHash ?? new byte[0]);
        }

        private static LogEntry WrapObject<T>(ulong type, ulong version, T inner, byte[] previousHash)
            where T : ILogSerialized
        {
            var @object = new LogObject
            {
                Timestamp = TimestampFactory.Now,
                Type = type,
                Version = version,
                Data = inner
            };

            @object.Hash = HashProvider.ComputeHashBytes(@object);

            var entry = new LogEntry
            {
                PreviousHash = previousHash,
                Timestamp = @object.Timestamp,
                Nonce = Crypto.Nonce(64U),
                Objects = new[] {@object}
            };

            entry.HashRoot = HashProvider.ComputeHashRootBytes(entry);
            entry.Hash = HashProvider.ComputeHashBytes(entry);
            return entry;
        }


        public static LogEntry CreateEntry<T>(T data, byte[] previousHash = default,
            ulong formatVersion = LogSerializeContext.FormatVersion) where T : ILogSerialized
        {
            var type = TypeProvider.Get(data.GetType()).GetValueOrDefault();
            return WrapObject(type, formatVersion, data, previousHash ?? new byte[0]);
        }
    }
}