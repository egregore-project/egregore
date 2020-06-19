// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore
{
    public interface ILogObjectTypeProvider
    {
        ulong? Get(Type type);
        Type Get(ulong type);
        ILogSerialized Deserialize(Type type, LogDeserializeContext context);
    }
}