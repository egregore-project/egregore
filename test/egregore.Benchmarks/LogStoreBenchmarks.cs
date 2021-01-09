// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Cryptography;
using egregore.Ontology;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31)]
    [SkewnessColumn]
    [KurtosisColumn]
    [MarkdownExporter]
    [MemoryDiagnoser]
    public class LogStoreBenchmarks
    {
        private LogEntryHashProvider _hashProvider;
        private byte[] _previousHash;
        private LightningLogStore _store;
        private LogObjectTypeProvider _typeProvider;

        [GlobalSetup]
        public void Setup()
        {
            _typeProvider = new LogObjectTypeProvider();
            _hashProvider = new LogEntryHashProvider(_typeProvider);
            _store = new LightningLogStore(_typeProvider);
            _previousHash = default;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _store.Destroy();
        }

        [Benchmark]
        public async Task Write_speed()
        {
            var inner = new Namespace("Benchmark");

            var @object = new LogObject
            {
                Timestamp = new UInt128(0UL, (ulong) DateTimeOffset.Now.ToUnixTimeSeconds()),
                Type = _typeProvider.Get(typeof(Namespace)),
                Version = 1,
                Data = inner
            };

            @object.Hash = _hashProvider.ComputeHashBytes(@object);

            var entry = new LogEntry
            {
                PreviousHash = _previousHash,
                Timestamp = @object.Timestamp,
                Nonce = Crypto.Nonce(64U),
                Objects = new[] {@object}
            };

            entry.HashRoot = _hashProvider.ComputeHashRootBytes(entry);
            entry.Hash = _hashProvider.ComputeHashBytes(entry);

            await _store.AddEntryAsync(entry);

            _previousHash = entry.Hash;
        }
    }
}