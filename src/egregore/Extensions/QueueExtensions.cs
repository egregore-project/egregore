// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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