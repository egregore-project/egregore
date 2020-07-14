// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31), SkewnessColumn, KurtosisColumn, MarkdownExporter, MemoryDiagnoser]
    public class SocketBenchmarks
    {
        private SocketServer _server;
        private SocketClient _client;

        [GlobalSetup]
        public void Setup()
        {
            _server = new SocketServer(new EchoProtocol(false));
            _server.Start(11000);
            
            _client = new SocketClient(new EchoProtocol(true));
            _client.Connect("localhost", 11000);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _client.Disconnect();
            _server.Dispose();
        }
        
        [Benchmark]
        public void Send_and_receive_loop()
        {
            _client.Send("This is a message");
            _client.Receive();
        }
    }
}