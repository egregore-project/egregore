// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore
{
    public class ServerOptions
    {
        public byte[] PublicKey { get; set; }
        public byte[] SecretKey { get; set; } // FIX: need to lock memory and prevent paging
        public string EggPath { get; set; }
    }
}