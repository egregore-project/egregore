// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Network;

namespace egregore
{
    internal sealed class GlobalSequenceProvider : ISequenceProvider
    {
        private readonly Sequence _sequence;

        public GlobalSequenceProvider(string name = Constants.DefaultSequence)
        {
            _sequence = new Sequence(Constants.DefaultRootPath, name);
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