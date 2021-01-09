// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Data;

namespace egregore.Media
{
    public class MediaEntry : ILogSerialized
    {
        public MediaEntry()
        {
        }

        public Guid Uuid { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public ulong Length { get; set; }
        public byte[] Data { get; set; }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Uuid);
            context.bw.Write(Type);
            context.bw.Write(Name);
            context.bw.Write(Length);
            context.bw.WriteVarBuffer(Data);
        }

        public MediaEntry(LogDeserializeContext context)
        {
            Uuid = context.br.ReadGuid();
            Type = context.br.ReadString();
            Name = context.br.ReadString();
            Length = context.br.ReadUInt64();
            Data = context.br.ReadVarBuffer();
        }

        #endregion
    }
}