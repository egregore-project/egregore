// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public ulong Version { get; set; }
        public byte[] PreviousHash { get; set; }
        public byte[] HashRoot { get; set; }
        public UInt128 Timestamp { get; set; }

        public ulong? Index { get; set; }
        public byte[] Nonce { get; set; }
        public byte[] Hash { get; internal set; }

        public IList<LogObject> Objects { get; set; }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            LogHeader.Serialize(this, context, hash);
            if(!hash)
                context.bw.WriteVarBuffer(Hash);
            SerializeObjects(context, hash);
        }

        private LogEntry(LogDeserializeContext context)
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
                using var dms = new MemoryStream(SecretStream.DecryptMessage(deserializeContext.br.ReadVarBuffer(), nonce, secretKey));
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
    }
}