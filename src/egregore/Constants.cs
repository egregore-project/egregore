// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using egregore.IO;

namespace egregore
{
    internal static class Constants
    {
        public const string DefaultNamespace = "Default";
        public const string DefaultSequence = "global";
        public const string DefaultOwnerRole = "owner";

        public const int DefaultPort = 5001;
        
        public static readonly string DefaultKeyFilePath = Path.Combine(".egregore", "egregore.key");
        public static readonly string DefaultEggPath = Path.Combine(".egregore", "default.egg");
        public static readonly IKeyCapture ConsoleKeyCapture = new ConsoleKeyCapture();

        public static class EnvVars
        {
            public const string KeyFilePassword = "EGREGORE_KEY_FILE_PASSWORD";
            public const string EggFilePath = "EGREGORE_EGG_FILE_PATH";
        }

        public class Commands
        {
            public const string GrantRole = "grant_role";
            public const string RevokeRole = "revoke_role";
        }
    }
}