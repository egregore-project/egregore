// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore
{
    public interface ILogSerialized
    {
        void Serialize(LogSerializeContext context, bool hash);
    }
}