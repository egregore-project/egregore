// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.InteropServices;

namespace egregore
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UInt128
    {
        public readonly ulong v1;
        public readonly ulong v2;

        public UInt128(ulong v1, ulong v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        internal UInt128(string value) : this(Convert.ToUInt64(value.Substring(0, 8), 8),
            Convert.ToUInt64(value.Substring(8, 8), 16))
        {
        }

        public static bool operator ==(UInt128 a, UInt128 b)
        {
            return a.v1 == b.v1 && a.v2 == b.v2;
        }

        public static bool operator !=(UInt128 a, UInt128 b)
        {
            return !(a == b);
        }

        public static UInt128 operator ^(UInt128 a, UInt128 b)
        {
            return new UInt128(a.v1 ^ b.v1, a.v2 ^ b.v2);
        }

        public static implicit operator UInt128(string id)
        {
            return new UInt128(id);
        }

        public override bool Equals(object obj)
        {
            return obj is UInt128 o && o == this;
        }

        public override int GetHashCode()
        {
            return (int) (v1 ^ v2);
        }

        public override string ToString()
        {
            return $"{v1:X8}{v2:X8}";
        }
    }
}