// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore
{
    public interface IPersistedKeyCapture : IKeyCapture, IDisposable
    {
        unsafe void Sink(byte* password, int passwordLength)
        {
            throw new NotSupportedException();
        }

        unsafe bool TryReadPersisted(out byte* password, out int passwordLength)
        {
            throw new NotSupportedException();
        }
    }
}