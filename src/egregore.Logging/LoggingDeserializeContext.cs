// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace egregore.Logging
{
    public sealed class LoggingDeserializeContext
    {
        public readonly BinaryReader br;

        public LoggingDeserializeContext(BinaryReader br)
        {
            this.br = br;
            Version = br.ReadUInt64();
            if (Version > LoggingSerializeContext.FormatVersion)
                throw new Exception("Tried to load log entry with a version that is too new");
        }

        public ulong Version { get; }
    }
}