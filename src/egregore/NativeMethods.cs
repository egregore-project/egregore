// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// ReSharper disable IdentifierTypo

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace egregore
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Binding")]
    internal static class NativeMethods
    {
        public const string DllName = "libsodium";

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/generating_random_data" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void randombytes_buf(byte* buf, uint size);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/public-key_cryptography/public-key_signatures" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int crypto_sign_keypair(byte* pk, byte* sk);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/helpers#hexadecimal-encoding-decoding" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr sodium_bin2hex(byte* hex, int hexMaxlen, byte* bin, int binLen);
    }
}