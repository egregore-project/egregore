// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace egregore.Tests
{
    public static class WifFormatterTests
    {
        [Fact]
        public static void Can_round_trip_key_pair_with_WIF()
        {
            var (pk, sk) = Crypto.GenerateKeyPairDangerous();
            Assert.NotEmpty(pk);
            Assert.NotEmpty(sk);

            var wif = WifFormatter.Serialize(sk);
            Assert.NotNull(wif);
            Assert.NotEmpty(wif);

            var (pk2, sk2) = WifFormatter.Deserialize(wif);
            Assert.True(pk.SequenceEqual(pk2));
            Assert.True(sk.SequenceEqual(sk2));
        }
    }
}