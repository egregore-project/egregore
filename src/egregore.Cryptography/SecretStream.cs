// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Cryptography
{
    /// <summary>
    ///     https://libsodium.gitbook.io/doc/secret-key_cryptography/secretstream
    /// </summary>
    public static class SecretStream
    {
        public static byte[] Nonce()
        {
            throw new NotImplementedException();
        }

        public static byte[] EncryptMessage(byte[] message, byte[] nonce, byte[] secretKey)
        {
            throw new NotImplementedException();
        }

        public static byte[] DecryptMessage(byte[] message, byte[] nonce, byte[] secretKey)
        {
            throw new NotImplementedException();
        }
    }
}