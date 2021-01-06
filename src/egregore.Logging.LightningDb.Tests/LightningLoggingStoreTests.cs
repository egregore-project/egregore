using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        public async Task Empty_store_has_zero_length()
        {
            using var store = new LightningLoggingStore();
            store.Init(_tempDir.NewDirectory());
            
            var length = await store.GetLengthAsync(CancellationToken.None);
            Assert.Equal(0, length);
        }

        [Fact]
        public async Task Can_append_to_store()
        {
            var directory = _tempDir.NewDirectory();

            var store1 = new LightningLoggingStore();
            store1.Init(directory);

            Assert.Equal(0, await store1.GetLengthAsync(CancellationToken.None));
            store1.Append<object>(LogLevel.Debug, new EventId(0), default, default, (s, e) => "this is a log entry");
            Assert.Equal(3, await store1.GetLengthAsync(CancellationToken.None)); /* 3 keys per log entry */
            store1.Dispose();

            var store2 = new LightningLoggingStore();
            store2.Init(directory);

            Assert.Equal(3, await store2.GetLengthAsync(CancellationToken.None));
            store2.Append<object>(LogLevel.Debug, new EventId(0), default, default, (s, e) => "this is another log entry");
            Assert.Equal(6, await store2.GetLengthAsync(CancellationToken.None)); /* 3 keys per log entry */
            store2.Dispose();
        }

        public void Dispose()
        {
            _tempDir?.Dispose();
        }
    }
}