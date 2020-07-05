// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace egregore
{
    internal static class Constants
    {
        public static readonly string DefaultNamespace = "Default";
        public static readonly string DefaultKeyPath = Path.Combine(".egregore", "egregore.key");
        public static readonly string DefaultEggPath = "default.egg";
        public static readonly string OwnerRole = "owner";

        public static readonly IKeyCapture ConsoleKeyCapture = new ConsoleKeyCapture();
    }
}