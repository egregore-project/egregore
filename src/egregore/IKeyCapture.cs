using System;
using System.IO;

namespace egregore
{
    public interface IKeyCapture
    {
        ConsoleKeyInfo ReadKey();
        void Reset();
        void OnKeyRead(TextWriter @out);
    }
}