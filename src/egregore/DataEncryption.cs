// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore
{
    internal static class DataEncryption
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