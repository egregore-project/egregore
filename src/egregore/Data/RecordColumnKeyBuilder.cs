// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace egregore.Data
{
    internal sealed class RecordColumnKeyBuilder
    {
        public byte[] BuildColumnKey(Record record, RecordColumn column)
        {
            return Encoding.UTF8.GetBytes($"C:{record.Type}:{column.Name}:{column.Value}");
        }

        public byte[] ReverseKey(string type, string name, string value)
        {
            return Encoding.UTF8.GetBytes($"C:{type}:{name}:{value}");
        }
    }
}