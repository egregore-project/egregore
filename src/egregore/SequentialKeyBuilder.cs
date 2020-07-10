// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore
{
    internal sealed class SequentialKeyBuilder
    {
        public byte[] BuildKey<T>(ulong sequence, T state) => BitConverter.GetBytes(sequence);
    }
}