﻿using BenchmarkDotNet.Running;

namespace egregore.Logging.LightningDb.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
