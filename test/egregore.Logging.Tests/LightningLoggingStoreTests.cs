using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Xunit;

namespace egregore.Logging.Tests
{
    [Collection(nameof(TempDirectory))]
    public sealed class LightningLoggingStoreTests : IDisposable
    {
        private readonly TempDirectory _tempDir;

        public LightningLoggingStoreTests(TempDirectory tempDir)
        {
            _tempDir = tempDir;
        }

        [Fact]
        public void Empty_store_has_zero_length()
        {
            WithOpenStore(async store =>
            {
                var length = await store.GetLengthAsync(CancellationToken.None);
                Assert.Equal(0, length);
            });
        }

        [Fact]
        public void Can_append_to_store()
        {
            var directory = _tempDir.NewDirectory();

            // first pass is empty
            WithOpenStore(directory, async store =>
            {
                Assert.Equal(0, await store.GetLengthAsync(CancellationToken.None));
                store.Append<object>(LogLevel.Debug, new EventId(0), default, default, (s, e) => "this is a log entry");
                Assert.Equal(3, await store.GetLengthAsync(CancellationToken.None)); /* 3 keys per log entry */
                store.Dispose();
            });

            // second pass is populated with first entry
            WithOpenStore(directory, async store =>
            {
                store.Init(directory);
                Assert.Equal(3, await store.GetLengthAsync(CancellationToken.None));
                store.Append<object>(LogLevel.Debug, new EventId(0), default, default, (s, e) => "this is another log entry");
                Assert.Equal(6, await store.GetLengthAsync(CancellationToken.None)); /* 3 keys per log entry */
                store.Dispose();
            });
        }

        private void WithOpenStore(Action<LightningLoggingStore> action)
        {
            using var store = new LightningLoggingStore();
            store.Init(_tempDir.NewDirectory());
            action(store);
        }

        private static void WithOpenStore(string directory, Action<LightningLoggingStore> action)
        {
            using var store = new LightningLoggingStore();
            store.Init(directory);
            action(store);
        }

        public void Dispose()
        {
            _tempDir?.Dispose();
        }
    }
}