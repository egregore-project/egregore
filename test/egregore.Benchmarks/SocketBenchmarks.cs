// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;
using Noise;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31), SkewnessColumn, KurtosisColumn, MarkdownExporter, MemoryDiagnoser]
    public class SocketBenchmarks
    {
        private SocketServer _echoServer;
        private SocketClient _echoClient;

        private SocketServer _noiseServer;
        private SocketClient _noiseClient;

        [GlobalSetup]
        public void Setup()
        {
            _echoServer = new SocketServer(new EchoProtocol(false));
            _echoServer.Start(11000);
            
            _echoClient = new SocketClient(new EchoProtocol(true));
            _echoClient.Connect("localhost", 11000);

            unsafe
            {
                var ckp = KeyPair.Generate();
                var skp = KeyPair.Generate();

                var psk1 = PskRef.Create();
                var psk2 = PskRef.Create(psk1.ptr);
                
                var sp = new NoiseProtocol(false, skp.PrivateKey, psk1);
                _noiseServer = new SocketServer(sp);
                _noiseServer.Start(12000);
            
                var cp = new NoiseProtocol(true, ckp.PrivateKey, psk2, skp.PublicKey);
                _noiseClient = new SocketClient(cp);
                _noiseClient.Connect("localhost", 12000);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _echoClient.Disconnect();
            _echoServer.Dispose();

            _noiseClient.Disconnect();
            _noiseServer.Dispose();
        }
        
        [Benchmark(Baseline = true)]
        public void Send_and_receive_loop_echo()
        {
            _echoClient.Send("This is a message");
            _echoClient.Receive();
        }

        [Benchmark(Baseline = false)]
        public void Send_and_receive_loop_noise()
        {
            _noiseClient.Send("This is a message");
            _noiseClient.Receive();
        }
    }
}