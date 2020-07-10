// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using egregore.Network;

namespace egregore
{
    internal sealed class GlobalSequenceProvider : ISequenceProvider
    {
        private readonly Sequence _sequence;

        public GlobalSequenceProvider(string name = Constants.DefaultSequence)
        {
            _sequence = new Sequence(name);
        }

        public Task<ulong> GetNextValueAsync()
        {
            return Task.FromResult((ulong) _sequence.GetNextValue());
        }

        public void Destroy()
        {
            _sequence.Destroy();
            Dispose();
        }

        public void Dispose()
        {
            _sequence.Dispose();
        }
    }
}