// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using egregore.Extensions;

namespace egregore.Data
{
    public sealed class Record : ILogSerialized
    {
        public Record()
        {
            Columns = new List<RecordColumn>();
        }

        public ulong? Index { get; set; }
        public Guid Uuid { get; set; }
        public string Type { get; set; }
        public ulong TimestampV1 { get; set; }
        public ulong TimestampV2 { get; set; }

        public List<RecordColumn> Columns { get; }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Uuid);
            context.bw.Write(Type);
            context.bw.Write(TimestampV1);
            context.bw.Write(TimestampV2);

            context.bw.Write(Columns.Count);
            foreach (var column in Columns)
                column.Serialize(context, hash);
        }

        public Record(LogDeserializeContext context) : this()
        {
            Uuid = context.br.ReadGuid();
            Type = context.br.ReadString();
            TimestampV1 = context.br.ReadUInt64();
            TimestampV2 = context.br.ReadUInt64();

            var columns = context.br.ReadInt32();
            for (var i = 0; i < columns; i++)
                Columns.Add(new RecordColumn(context));
        }

        public Record(Guid id, LogDeserializeContext context) : this()
        {
            Uuid = id;
            Type = context.br.ReadString();
            TimestampV1 = context.br.ReadUInt64();
            TimestampV2 = context.br.ReadUInt64();

            var columns = context.br.ReadInt32();
            for (var i = 0; i < columns; i++)
                Columns.Add(new RecordColumn(context));
        }

        #endregion
    }
}