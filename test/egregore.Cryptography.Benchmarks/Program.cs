﻿using BenchmarkDotNet.Running;

namespace egregore.Cryptography.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
