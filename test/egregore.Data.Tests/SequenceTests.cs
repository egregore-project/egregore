// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using egregore.Data.Tests.Fixtures;
using Xunit;

namespace egregore.Data.Tests
{
    [Collection("Serial")]
    public sealed class SequenceTests : IClassFixture<SequenceFixture>
    {
        public SequenceTests(SequenceFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly SequenceFixture _fixture;

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(1, 2, 3)]
        [InlineData(2, 1, 3)]
        [InlineData(2, 2, 4)]
        public void Can_start_with_and_increment_by(long startWith, long incrementBy, long afterIncrement)
        {
            var sequenceName = $"{Guid.NewGuid()}";
            try
            {
                using var sequence = new Sequence(string.Empty, sequenceName, startWith, incrementBy);
                Assert.Equal(afterIncrement, sequence.GetNextValue());
            }
            finally
            {
                var destroyMe = new Sequence(string.Empty, sequenceName);
                using (destroyMe)
                {
                }

                destroyMe.Destroy();
            }
        }

        [Fact]
        public void Can_increment()
        {
            Assert.Equal(-1, _fixture.Sequence.Current);
            Assert.Equal(0, _fixture.Sequence.GetNextValue());
            Assert.Equal(0, _fixture.Sequence.Current);
        }

        [Fact]
        public async void Count_survives_scoping()
        {
            var sequenceName = $"{Guid.NewGuid()}";

            try
            {
                var one = Task.Run(() =>
                {
                    using var sequence = new Sequence(string.Empty, sequenceName);
                    sequence.GetNextValue();
                });

                var two = Task.Run(() =>
                {
                    using var sequence = new Sequence(string.Empty, sequenceName);
                    sequence.GetNextValue();
                });

                var three = Task.Run(() =>
                {
                    using var sequence = new Sequence(string.Empty, sequenceName);
                    sequence.GetNextValue();
                });

                await three;
                await two;
                await one;

                using (var sequence = new Sequence(string.Empty, sequenceName))
                {
                    Assert.Equal(2, sequence.Current);
                }
            }
            finally
            {
                var destroyMe = new Sequence(string.Empty, sequenceName);
                using (destroyMe)
                {
                }

                destroyMe.Destroy();
            }
        }

        [Fact]
        public async void Increasing_startWith_throws()
        {
            var sequenceName = $"{Guid.NewGuid()}";

            try
            {
                using (var sequence = new Sequence(string.Empty, sequenceName, 1))
                {
                    Assert.Equal(2, sequence.GetNextValue());
                }

                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                {
                    return Task.Run(() =>
                    {
                        using (new Sequence(string.Empty, sequenceName, 5))
                        {
                        }
                    });
                });
            }
            finally
            {
                var destroyMe = new Sequence(string.Empty, sequenceName);
                using (destroyMe)
                {
                }

                destroyMe.Destroy();
            }
        }
    }
}