// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Data
{
    public static class TimestampFactory
    {
        public static UInt128 Now => new UInt128(0UL, (ulong) DateTimeOffset.Now.ToUnixTimeSeconds());

        public static DateTimeOffset FromUInt128(UInt128 value)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long) value.v2);
        }

        public static DateTimeOffset FromUInt64(ulong value)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long) value);
        }
    }
}