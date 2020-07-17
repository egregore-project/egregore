// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Running;

namespace egregore.Benchmarks
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            //BenchmarkRunner.Run<SequenceBenchmarks>();
            //BenchmarkRunner.Run<LogStoreBenchmarks>();
            BenchmarkRunner.Run<SocketBenchmarks>();
        }
    }
}