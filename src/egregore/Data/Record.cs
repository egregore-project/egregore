// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace egregore.Data
{
    public sealed class Record : ILogSerialized
    {
        public ulong? Index { get; set; }
        public string Type { get; set; }
        public Guid Uuid { get; set; }
        public List<RecordColumn> Columns { get; }

        public Record()
        {
            Columns = new List<RecordColumn>();
        }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Type);
            context.bw.Write(Uuid.ToByteArray());
            context.bw.Write(Columns.Count);
            foreach (var column in Columns)
                column.Serialize(context, hash);
        }

        public Record(LogDeserializeContext context)
        {
            Type = context.br.ReadString();
            Uuid = new Guid(context.br.ReadBytes(16));
            for(var i = 0; i < context.br.ReadInt32(); i++)
                Columns.Add(new RecordColumn(context));
        }

        #endregion
    }
}