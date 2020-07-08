using System;
using System.IO;

namespace egregore.IO
{
    internal sealed class ConsoleKeyCapture : IKeyCapture
    {
        public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);
        public void Reset() => Console.Clear();
        public void OnKeyRead(TextWriter @out) => @out.Write(Strings.PasswordMask);
    }
}