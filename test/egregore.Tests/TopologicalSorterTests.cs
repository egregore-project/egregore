using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class TopologicalSorterTests
    {
        private readonly ITestOutputHelper _output;

        public TopologicalSorterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Can_sort()
        {
            var list = new List<string> {"A", "B", "C"};

            var edges = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("C", "A"),  // A depends on C
                new Tuple<string, string>("B", "C")   // C depends on B
            };
            
            var sorted = TopologicalSorter<string>.Sort(list, edges);
            Assert.NotNull(sorted);

            foreach(var entry in sorted)
                _output.WriteLine(entry);
        }
    }
}
