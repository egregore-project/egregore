// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore
{
    public sealed class LogException : Exception
    {
        public LogException(string message) : base(message)
        {
        }
    }
}