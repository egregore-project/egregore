// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using WyHash;

namespace egregore.Extensions
{
    internal static class ETagExtensions
    {
        private static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(ETagExtensions)));

        public static string StrongETag(this byte[] data)
        {
            var value = WyHash64.ComputeHash64(data, Seed);
            return $"\"{value}\"";
        }

        public static string WeakETag(this string key, bool prefix)
        {
            var value = WyHash64.ComputeHash64(Encoding.UTF8.GetBytes(key), Seed);
            return prefix ? $"W/\"{value}\"" : $"\"{value}\"";
        }
    }
}