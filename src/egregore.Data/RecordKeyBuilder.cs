﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;

namespace egregore.Data
{
    internal sealed class RecordKeyBuilder
    {
        public byte[] BuildRecordKey(Record record)
        {
            return ReverseRecordKey(record.Uuid);
        }

        public byte[] ReverseRecordKey(Guid uuid)
        {
            return Encoding.UTF8.GetBytes(string.Concat("R:", uuid));
        }

        public byte[] AllRecordsKey()
        {
            return Encoding.UTF8.GetBytes("R:");
        }

        public byte[] BuildTypeToIndexKey(Record record)
        {
            return Encoding.UTF8.GetBytes($"T:{record.Type}:{record.Index}");
        }

        public byte[] ReverseTypeKey(string type)
        {
            return Encoding.UTF8.GetBytes($"T:{type}");
        }
    }
}