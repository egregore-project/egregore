// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace egregore.Data
{
    public sealed class RecordColumn : ILogSerialized
    {
        public RecordColumn(int index, string name, string type, string value)
        {
            Index = index;
            Name = name;
            Type = type;
            Value = value;
        }

        public int Index { get; }
        public string Name { get; }
        public string Type { get; }
        public string Value { get; }
        public string Default { get; set; }

        public static IComparer<RecordColumn> IndexComparer { get; } = new IndexRelationalComparer();

        private sealed class IndexRelationalComparer : IComparer<RecordColumn>
        {
            public int Compare(RecordColumn x, RecordColumn y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Index.CompareTo(y.Index);
            }
        }

        #region Serialization

        public void Serialize(LogSerializeContext context, bool hash)
        {
            throw new NotImplementedException();
        }

        public RecordColumn(LogSerializeContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}