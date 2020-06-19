// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore
{
    public interface ILogDescriptor
    {
        int Version { get; set; }
        byte[] PreviousHash { get; set; }
        byte[] HashRoot { get; set; }
        UInt128 Timestamp { get; set; }
        byte[] Nonce { get; set; }
    }
}