// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace egregore.IO
{
    internal sealed class ConsoleKeyCapture : IKeyCapture
    {
        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey(true);
        }

        public void Reset()
        {
            Console.Clear();
        }

        public void OnKeyRead(TextWriter @out)
        {
            @out.Write(Strings.PasswordMask);
        }
    }
}