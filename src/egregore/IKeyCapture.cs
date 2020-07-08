// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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