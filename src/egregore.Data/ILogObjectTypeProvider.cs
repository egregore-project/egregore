﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Data
{
    public interface ILogObjectTypeProvider
    {
        ulong? Get(Type type);
        Type Get(ulong type);
        ILogSerialized Deserialize(Type type, LogDeserializeContext context);
    }
}