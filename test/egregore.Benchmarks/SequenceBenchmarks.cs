// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31)]
    [SkewnessColumn]
    [KurtosisColumn]
    [MarkdownExporter]
    [MemoryDiagnoser]
    public class SequenceBenchmarks
    {
        private Sequence _sequence;

        [GlobalSetup]
        public void Setup()
        {
            _sequence = new Sequence($"{Guid.NewGuid()}");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _sequence.Destroy();
        }

        [Benchmark]
        public void Write_speed()
        {
            _sequence.GetNextValue();
        }
    }
}