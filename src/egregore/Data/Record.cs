// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace egregore.Data
{
    public sealed class Record : ILogSerialized
    {
        public Record()
        {
            Columns = new List<RecordColumn>();
        }

        public ulong? Index { get; set; }
        public string Type { get; set; }
        public Guid Uuid { get; set; }
        public List<RecordColumn> Columns { get; }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Type);
            context.bw.Write(Uuid.ToByteArray());
            context.bw.Write(Columns.Count);
            foreach (var column in Columns)
                column.Serialize(context, hash);
        }

        public Record(LogDeserializeContext context) : this()
        {
            Type = context.br.ReadString();
            Uuid = new Guid(context.br.ReadBytes(16));
            var columns = context.br.ReadInt32();
            for (var i = 0; i < columns; i++)
                Columns.Add(new RecordColumn(context));
        }

        #endregion
    }
}