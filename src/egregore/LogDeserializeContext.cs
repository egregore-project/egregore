// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace egregore
{
    public sealed class LogDeserializeContext
    {
        public readonly BinaryReader br;
        public ILogObjectTypeProvider typeProvider;


        public LogDeserializeContext(BinaryReader br, ILogObjectTypeProvider typeProvider)
        {
            this.br = br;
            this.typeProvider = typeProvider;

            Version = br.ReadInt32();

            if (Version > LogSerializeContext.FormatVersion)
                throw new Exception("Tried to load block with a version that is too new");
        }

        public int Version { get; }
    }
}