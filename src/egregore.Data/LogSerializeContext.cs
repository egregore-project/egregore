﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace egregore.Data
{
    public sealed class LogSerializeContext
    {
        public const ulong FormatVersion = 1UL;

        public readonly BinaryWriter bw;
        public ILogObjectTypeProvider typeProvider;

        public LogSerializeContext(BinaryWriter bw, ILogObjectTypeProvider typeProvider, ulong version = FormatVersion)
        {
            this.bw = bw;
            this.typeProvider = typeProvider;
            if (Version > FormatVersion)
                throw new Exception("Tried to save block with a version that is too new");
            Version = version;

            bw.Write(Version);
        }

        public ulong Version { get; }
    }
}