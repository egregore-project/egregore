// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using egregore.Extensions;

namespace egregore
{
    public class LogEntry : ILogDescriptor
    {
        public LogEntry()
        {
            Version = 1;
            Objects = new List<LogObject>();
        }

        public ulong? Index { get; set; }
        public byte[] Hash { get; internal set; }

        public IList<LogObject> Objects { get; set; }

        public ulong Version { get; set; }
        public byte[] PreviousHash { get; set; }
        public byte[] HashRoot { get; set; }
        public UInt128 Timestamp { get; set; }
        public byte[] Nonce { get; set; }

        #region Serialization

        public byte[] SerializeObjects(ILogObjectTypeProvider typeProvider, byte[] secretKey = default)
        {
            RoundTripCheck(typeProvider, secretKey);

            byte[] data;
            using (var ms = new MemoryStream())
            {
                using var bw = new BinaryWriter(ms, Encoding.UTF8);

                // Version:
                var context = new LogSerializeContext(bw, typeProvider);

                if (secretKey != null)
                {
                    // Nonce:
                    var nonce = SecretStream.Nonce();
                    context.bw.WriteVarBuffer(nonce);

                    // Data:
                    using var ems = new MemoryStream();
                    using var ebw = new BinaryWriter(ems, Encoding.UTF8);
                    var ec = new LogSerializeContext(ebw, typeProvider, context.Version);
                    SerializeObjects(ec, false);

                    var message = SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey);
                    context.bw.WriteVarBuffer(message);
                }
                else
                {
                    // Data:
                    context.bw.Write(false);
                    SerializeObjects(context, false);
                }

                data = ms.ToArray();
            }

            return data;
        }

        public void DeserializeObjects(ILogObjectTypeProvider typeProvider, byte[] data, byte[] secretKey)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            // Version:
            var context = new LogDeserializeContext(br, typeProvider);

            // Nonce:
            var nonce = context.br.ReadVarBuffer();
            if (nonce != null)
            {
                var message = SecretStream.DecryptMessage(context.br.ReadVarBuffer(), nonce, secretKey);

                // Data:
                using var dms = new MemoryStream(message);
                using var dbr = new BinaryReader(dms);
                var dc = new LogDeserializeContext(dbr, typeProvider);
                DeserializeObjects(dc);
            }
            else
            {
                // Data:
                DeserializeObjects(context);
            }
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            LogHeader.Serialize(this, context, hash);
            if (!hash)
                context.bw.WriteVarBuffer(Hash);
            SerializeObjects(context, hash);
        }

        internal LogEntry(LogDeserializeContext context)
        {
            LogHeader.Deserialize(this, context);
            Hash = context.br.ReadVarBuffer();
            DeserializeObjects(context);
        }

        internal void DeserializeObjects(LogDeserializeContext context)
        {
            var count = context.br.ReadInt32();
            Objects = new List<LogObject>(count);
            for (var i = 0; i < count; i++)
            {
                var o = new LogObject(context);
                Objects.Add(o);
            }
        }

        internal void SerializeObjects(LogSerializeContext context, bool hash)
        {
            var count = Objects?.Count ?? 0;
            context.bw.Write(count);
            if (Objects != null)
                foreach (var @object in Objects.OrderBy(x => x.Index))
                    @object.Serialize(context, hash);
        }

        public void RoundTripCheck(ILogObjectTypeProvider typeProvider, byte[] secretKey)
        {
            var firstMemoryStream = new MemoryStream();
            var firstSerializeContext = new LogSerializeContext(new BinaryWriter(firstMemoryStream), typeProvider);

            byte[] nonce;
            if (secretKey != null)
            {
                nonce = SecretStream.Nonce();
                firstSerializeContext.bw.WriteVarBuffer(nonce);
                using var ems = new MemoryStream();
                using var ebw = new BinaryWriter(ems);
                var ec = new LogSerializeContext(ebw, typeProvider, firstSerializeContext.Version);
                Serialize(ec, false);
                firstSerializeContext.bw.WriteVarBuffer(SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey));
            }
            else
            {
                firstSerializeContext.bw.Write(false);
                Serialize(firstSerializeContext, false);
            }

            var originalData = firstMemoryStream.ToArray();

            var br = new BinaryReader(new MemoryStream(originalData));
            var deserializeContext = new LogDeserializeContext(br, typeProvider);
            nonce = deserializeContext.br.ReadVarBuffer();

            LogEntry deserialized;
            if (nonce != null)
            {
                using var dms =
                    new MemoryStream(SecretStream.DecryptMessage(deserializeContext.br.ReadVarBuffer(), nonce,
                        secretKey));
                using var dbr = new BinaryReader(dms);
                var dc = new LogDeserializeContext(dbr, typeProvider);
                deserialized = new LogEntry(dc);
            }
            else
            {
                deserialized = new LogEntry(deserializeContext);
            }

            var secondMemoryStream = new MemoryCompareStream(originalData);
            var secondSerializeContext = new LogSerializeContext(new BinaryWriter(secondMemoryStream), typeProvider);
            if (secretKey != null)
            {
                secondSerializeContext.bw.WriteVarBuffer(nonce);
                using var ems = new MemoryStream();
                using var ebw = new BinaryWriter(ems);
                var ec = new LogSerializeContext(ebw, typeProvider, secondSerializeContext.Version);
                deserialized.Serialize(ec, false);
                secondSerializeContext.bw.WriteVarBuffer(SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey));
            }
            else
            {
                secondSerializeContext.bw.Write(false);
                deserialized.Serialize(secondSerializeContext, false);
            }
        }

        #endregion

        #region Validation

        public void EntryCheck(LogEntry previous, HashProvider hashProvider)
        {
            if (previous.Index + 1 != Index)
            {
                var message = $"Invalid index: expected '{previous.Index + 1}' but was '{Index}'";
                throw new LogException(message);
            }

            if (!previous.Hash.SequenceEqual(PreviousHash))
            {
                var message = $"Invalid previous hash: expected '{Crypto.ToHexString(previous.Hash)}' but was '{Crypto.ToHexString(this.PreviousHash)}'";
                throw new LogException(message);
            }

            var hashRoot = hashProvider.ComputeHashRootBytes(this);
            if (!hashRoot.SequenceEqual(HashRoot))
            {
                var message = $"Invalid hash root: expected '{Crypto.ToHexString(hashRoot)}' but was '{Crypto.ToHexString(this.HashRoot)}'";
                throw new LogException(message);
            }

            for (var i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].Index == i)
                    continue;
                var message = $"Invalid object index: expected '{i}' but was '{this.Objects[i].Index}'";
                throw new LogException(message);
            }

            var hash = hashProvider.ComputeHashBytes(this);
            if (!hash.SequenceEqual(Hash))
            {
                var message = $"Invalid hash: expected '{Crypto.ToHexString(hash)}' but was '{Crypto.ToHexString(this.Hash)}'";
                throw new LogException(message);
            }
        }

        #endregion
    }
}