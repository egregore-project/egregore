// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace egregore
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