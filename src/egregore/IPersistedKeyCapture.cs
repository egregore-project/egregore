// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore
{
    public interface IPersistedKeyCapture : IKeyCapture, IDisposable
    {
        unsafe void Sink(byte* password, int passwordLength) => throw new NotSupportedException();
        unsafe bool TryReadPersisted(out byte* password, out int passwordLength) => throw new NotSupportedException();
    }
}