// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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

        public ulong? Index { get; set; }
        public byte[] Hash { get; internal set; }

        public IList<LogObject> Objects { get; set; }

        public ulong Version { get; set; }
        public byte[] PreviousHash { get; set; }
        public byte[] HashRoot { get; set; }
        public UInt128 Timestamp { get; set; }
        public byte[] Nonce { get; set; }
        
        #region Serialization

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
            if (Objects == null)
                return;
            foreach (var @object in Objects.OrderBy(x => x.Index))
                @object.Serialize(context, hash);
        }

        #endregion
    }
}