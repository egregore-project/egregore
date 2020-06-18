// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace egregore.Tests
{
    public class CryptoTests
    {
        [Fact]
        public void Can_generate_key_pair()
        {
            var pk = new byte[Crypto.PublicKeyBytes];
            var sk = new byte[Crypto.SecretKeyBytes];
            Crypto.GenerateKeyPair(pk, sk);
            Assert.NotEmpty(pk);
            Assert.NotEmpty(sk);
        }
    }
}