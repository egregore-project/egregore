﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace egregore.Data
{
    internal sealed class RecordKeyBuilder
    {
        public byte[] BuildKey(Record record)
        {
            return record.Uuid.ToByteArray();
        }
    }
}