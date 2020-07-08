// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace egregore.Extensions
{
    internal static class QueueExtensions
    {
        public static bool EndOfSubArguments(this Queue<string> arguments)
        {
            return arguments.Count == 0 || arguments.Peek().StartsWith("-");
        }
    }
}