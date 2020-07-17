// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Tests
{
    internal static class TimestampFactory
    {
        public static UInt128 Now => new UInt128(0UL, (ulong) DateTimeOffset.Now.ToUnixTimeSeconds());
    }
}