// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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