// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace egregore.Data
{
    public sealed class LogDeserializeContext
    {
        public readonly BinaryReader br;
        public ILogObjectTypeProvider typeProvider;

        public LogDeserializeContext(BinaryReader br, ILogObjectTypeProvider typeProvider)
        {
            this.br = br;
            this.typeProvider = typeProvider;

            Version = br.ReadUInt64();

            if (Version > LogSerializeContext.FormatVersion)
                throw new Exception("Tried to load block with a version that is too new");
        }

        public ulong Version { get; }
    }
}