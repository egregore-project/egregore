using System;

namespace egregore
{
    internal interface IKeyCapture
    {
        ConsoleKeyInfo ReadKey();
    }
}