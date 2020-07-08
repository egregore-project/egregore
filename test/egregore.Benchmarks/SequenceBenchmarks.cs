// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31), SkewnessColumn, KurtosisColumn, MarkdownExporter, MemoryDiagnoser]
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
            for(var i = 0; i < 1000; i++)
                _sequence.GetNextValue();
        }
    }
}