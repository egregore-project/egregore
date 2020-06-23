// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Ontology;

namespace egregore.Tests
{
    internal static class LogEntryFactory
    {
        public static readonly HashProvider HashProvider;
        public static readonly LogObjectTypeProvider TypeProvider;

        static LogEntryFactory()
        {
            TypeProvider = new LogObjectTypeProvider();
            HashProvider = new HashProvider(TypeProvider);
        }

        public static LogEntry CreateNamespaceEntry(string value, byte[] previousHash)
        {
            return WrapObject(TypeProvider.Get(typeof(Namespace)).GetValueOrDefault(), LogSerializeContext.FormatVersion, new Namespace(value), previousHash ?? new byte[0]);
        }

        public static LogEntry CreateGrantRoleEntry(string role, byte[] publicKey, byte[] signature, byte[] previousHash)
        {
            return WrapObject(TypeProvider.Get(typeof(GrantRole)).GetValueOrDefault(), LogSerializeContext.FormatVersion, 
                new GrantRole(role, publicKey, publicKey, signature), previousHash ?? new byte[0]);
        }

        public static LogEntry CreateRevokeRoleEntry(string role, byte[] publicKey, byte[] signature, byte[] previousHash)
        {
            return WrapObject(TypeProvider.Get(typeof(RevokeRole)).GetValueOrDefault(), LogSerializeContext.FormatVersion, 
                new RevokeRole(role, publicKey, publicKey, signature), previousHash ?? new byte[0]);
        }

        private static LogEntry WrapObject<T>(ulong type, ulong version, T inner, byte[] previousHash) where T : ILogSerialized
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


        public static LogEntry CreateEntry<T>(T data, byte[] previousHash = default, ulong formatVersion = LogSerializeContext.FormatVersion) where T : ILogSerialized
        {
            return WrapObject(TypeProvider.Get(data.GetType()).GetValueOrDefault(), formatVersion, data, previousHash ?? new byte[0]);
        }
    }
}