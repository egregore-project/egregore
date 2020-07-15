// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Configuration
{
    public class WebServerOptions
    {
        public byte[] PublicKey { get; set; }
        public string ServerId { get; set; }
        public string EggPath { get; set; }
        public short BeaconPort { get; set; }
    }
}