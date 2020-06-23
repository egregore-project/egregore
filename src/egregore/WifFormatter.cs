// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using egregore.Extensions;

namespace egregore
{
    /// <summary>
    /// <see href="https://en.bitcoin.it/wiki/Wallet_import_format" />
    /// <remarks>NEVER use this outside of offline key-pair generation methods or tests.</remarks>
    /// </summary>
    internal static class WifFormatter
    {
        private const byte Header = (byte) 'e';

        public static (byte[], byte[]) Deserialize(string wif)
        {
            var decoded = Base58Check.DecodePlain(wif);

            // Drop the last 4 checksum bytes from the byte string
            // Drop the first byte (it should be 0x80)
            // If the private key corresponded to a compressed public key, also drop the last byte (it should be 0x01)
            var secretKey = decoded.Take(decoded.Length - 5).Skip(1).ToArray();
            var publicKey = Crypto.PublicKeyFromSecretKeyDangerous(secretKey);
            return (publicKey, secretKey);
        }

        public static string Serialize(byte[] secretKey)
        {
            // Add a 0x80 byte in front of it for mainnet addresses or 0xef for testnet addresses. 
            // Also add a 0x01 byte at the end if the private key will correspond to a compressed public key
            var header = new byte[] {Header};
            var footer = new byte[] {0x01};
            var extendedKey = new byte[header.Length + secretKey.Length + footer.Length];
            Buffer.BlockCopy(header, 0, extendedKey, 0, header.Length);
            Buffer.BlockCopy(secretKey, 0, extendedKey, header.Length, secretKey.Length);
            Buffer.BlockCopy(footer, 0, extendedKey, header.Length + secretKey.Length, footer.Length);

            // Perform SHA-256 hash on the extended key
            // Perform SHA-256 hash on result of SHA-256 hash
            // Take the first 4 bytes of the second SHA-256 hash, this is the checksum;
            byte[] checksum =Crypto.Sha256(Crypto.Sha256(extendedKey)).Take(4).ToArray();

            // Add the 4 checksum bytes from point 5 at the end of the extended key from point 2
            byte[] exportKey = new byte[extendedKey.Length + checksum.Length];
            Buffer.BlockCopy(extendedKey, 0, exportKey, 0, extendedKey.Length);
            Buffer.BlockCopy(checksum, 0, exportKey, extendedKey.Length, checksum.Length);

            // Convert the result from a byte string into a base58 string using Base58Check encoding
            return Base58Check.EncodePlain(exportKey);
        }
    }
}