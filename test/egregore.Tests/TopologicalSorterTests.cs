// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class TopologicalSorterTests
    {
        public TopologicalSorterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        [Fact]
        public void Can_sort()
        {
            var list = new List<string> {"A", "B", "C"};

            var edges = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("C", "A"), // A depends on C
                new Tuple<string, string>("B", "C") // C depends on B
            };

            var sorted = TopologicalSorter<string>.Sort(list, edges);
            Assert.NotNull(sorted);

            foreach (var entry in sorted)
                _output.WriteLine(entry);
        }
    }
}