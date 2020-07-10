// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace egregore.Data
{
    internal sealed class RecordKeyBuilder
    {
        public byte[] BuildRecordKey(Record record) => ReverseRecordKey(record.Uuid);
        public byte[] ReverseRecordKey(Guid uuid) => Encoding.UTF8.GetBytes($"R:{uuid.ToByteArray()}");

        public byte[] BuildTypeKey(Record record) => ReverseTypeKey(record.Type);
        public byte[] ReverseTypeKey(string type) => Encoding.UTF8.GetBytes($"T:{type}");
    }
}