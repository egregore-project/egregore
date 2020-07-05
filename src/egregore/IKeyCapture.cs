using System;

namespace egregore
{
    public interface IKeyCapture
    {
        ConsoleKeyInfo ReadKey();
        void Reset();
    }
}