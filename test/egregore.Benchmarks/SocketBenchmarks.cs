// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31), SkewnessColumn, KurtosisColumn, MarkdownExporter, MemoryDiagnoser]
    public class SocketBenchmarks
    {
        private CancellationTokenSource _cancel;

        [GlobalSetup]
        public void Setup()
        {
            _cancel = new CancellationTokenSource();
            SocketServer.Start("localhost", 11000, _cancel.Token);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _cancel.Cancel(true);
            SocketServer.Stop();
        }
        
        [Benchmark]
        public void Send_and_receive_loop()
        {
            var client = new SocketClient();
            client.Start("localhost", 11000);
        }
    }
}