using System;

namespace egregore
{
    internal sealed class ConsoleKeyCapture : IKeyCapture
    {
        public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);
    }
}