// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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