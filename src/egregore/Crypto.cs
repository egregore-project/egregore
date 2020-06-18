using System;
using System.Runtime.InteropServices;

namespace egregore
{
    internal static class Crypto
    {
        public const uint PublicKeyBytes = 64U;
        public const uint SecretKeyBytes = 32U;

        static Crypto() => NativeLibrary.SetDllImportResolver(typeof(Crypto).Assembly, Minisign.VerifyImportResolver);

        public static void GenerateKeyPair(Span<byte> publicKey, Span<byte> secretKey)
        {
            unsafe
            {
                fixed(byte* pk = &publicKey.GetPinnableReference())
                    fixed(byte* sk = &secretKey.GetPinnableReference())
                        NativeMethods.crypto_sign_keypair(pk, sk);
            }
        }
    }

}
