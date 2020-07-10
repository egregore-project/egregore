// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace egregore
{
    public interface ISequenceProvider : IDisposable
    {
        Task<ulong> GetNextValueAsync();
        void Destroy();
    }
}