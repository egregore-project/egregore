// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Schema;

namespace egregore.Tests
{
    internal static class LogEntryFactory
    {
        private static readonly HashProvider HashProvider;

        static LogEntryFactory()
        {
            var typeProvider = new LogObjectTypeProvider();
            HashProvider = new HashProvider(typeProvider);
        }

        public static LogEntry CreateNamespaceEntry(string value, byte[] previousHash)
        {
            return WrapObject(Namespace.Type, Namespace.Version, new Namespace(value), previousHash ?? new byte[0]);
        }

        private static LogEntry WrapObject<T>(ulong type, ulong version, T inner, byte[] previousHash) where T : ILogSerialized
        {
            var @object = new LogObject
            {
                Index = 0UL,
                Timestamp = TimestampFactory.Now,
                Type = type,
                Version = version,
                Data = inner
            };

            @object.Hash = HashProvider.ComputeHashBytes(@object);

            var entry = new LogEntry
            {
                Index = 0UL,
                PreviousHash = previousHash,
                Timestamp = @object.Timestamp,
                Nonce = Crypto.Nonce(64U),
                Objects = new[] {@object}
            };

            entry.HashRoot = HashProvider.ComputeHashRootBytes(entry);
            entry.Hash = HashProvider.ComputeHashBytes(entry);
            return entry;
        }
    }
}