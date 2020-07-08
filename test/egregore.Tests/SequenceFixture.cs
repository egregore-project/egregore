// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Network;

namespace egregore.Tests
{
    public class SequenceFixture : IDisposable
    {
        public Sequence Sequence { get; }

        public SequenceFixture()
        {
            Sequence = new Sequence($"{Guid.NewGuid()}");
        }

        public void Dispose()
        {
            Sequence.Destroy();
        }
    }
}