// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// ReSharper disable IdentifierTypo

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace egregore
{
    internal static class NativeMethods
    {
        public const string DllName = "libsodium";

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/public-key_cryptography/public-key_signatures" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Binding")]
        public static extern unsafe int crypto_sign_keypair(byte* publicKey, byte* secretKey);
    }
}