using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace egregore.Logging.LightningDb.Tests
{
    [Collection(nameof(TempDirectory))]
    public class LightningLoggingStoreTests : IDisposable
    {
        private readonly TempDirectory _tempDir;

        public LightningLoggingStoreTests(TempDirectory tempDir)
        {
            _tempDir = tempDir;
        }

        [Fact]
        public async Task Empty_database_has_zero_length()
        {
            using var store = new LightningLoggingStore();
            store.Init(_tempDir.NewDirectory());
            
            var length = await store.GetLengthAsync(CancellationToken.None);
            Assert.Equal(0, length);
        }

        public void Dispose()
        {
            _tempDir?.Dispose();
        }
    }
}