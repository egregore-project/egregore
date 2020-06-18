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
