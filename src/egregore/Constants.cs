﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using egregore.IO;

namespace egregore
{
    internal static class Constants
    {
        public static readonly string DefaultNamespace = "Default";
        public static readonly string DefaultKeyFilePath = Path.Combine(".egregore", "egregore.key");
        public static readonly string DefaultEggPath = Path.Combine(".egregore", "default.egg");
        public static readonly string OwnerRole = "owner";

        public static readonly IKeyCapture ConsoleKeyCapture = new ConsoleKeyCapture();

        internal static class EnvVars
        {
            public const string KeyFilePassword = "EGREGORE_KEY_FILE_PASSWORD";
        }
    }
}