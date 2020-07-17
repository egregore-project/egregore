// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using egregore.Network;
using Noise;

namespace egregore.Benchmarks
{
    [MediumRunJob(RuntimeMoniker.NetCoreApp31)]
    [SkewnessColumn]
    [KurtosisColumn]
    [MarkdownExporter]
    [MemoryDiagnoser]
    public class SocketBenchmarks
    {
        private SocketClient _echoClient;
        private SocketServer _echoServer;
        private SocketClient _noiseClient;

        private SocketServer _noiseServer;

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