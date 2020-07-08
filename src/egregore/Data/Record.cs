// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace egregore.Data
{
    public sealed class Record : ILogSerialized
    {
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
            throw new NotImplementedException();
        }

        public Record(LogSerializeContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}