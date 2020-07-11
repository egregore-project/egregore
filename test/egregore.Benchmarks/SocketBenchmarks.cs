// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        private SocketServer _server;
        private SocketClient _client;

        [GlobalSetup]
        public void Setup()
        {
            _cancel = new CancellationTokenSource();
            _server = new SocketServer();
            _server.Start("localhost", 11000);
            _client = new SocketClient();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _cancel.Cancel(true);
            _server.Dispose();
        }
        
        [Benchmark]
        public void Send_and_receive_loop()
        {
            _client.ConnectAndSend("localhost", 11000);
        }
    }
}