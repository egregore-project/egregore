// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using egregore.Network;
using Xunit;

namespace egregore.Tests.Network
{
    public sealed class SequenceTests : IClassFixture<SequenceFixture>
    {
        private readonly SequenceFixture _fixture;

        public SequenceTests(SequenceFixture fixture)
        {
            _fixture = fixture;
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
                    using var sequence = new Sequence(sequenceName);
                    sequence.GetNextValue();
                });

                var two = Task.Run(() =>
                {
                    using var sequence = new Sequence(sequenceName);
                    sequence.GetNextValue();
                });

                var three = Task.Run(() =>
                {
                    using var sequence = new Sequence(sequenceName);
                    sequence.GetNextValue();
                });

                await three;
                await two;
                await one;

                using (var sequence = new Sequence(sequenceName))
                    Assert.Equal(2, sequence.Current);
            }
            finally
            {
                var destroyMe = new Sequence(sequenceName);
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
                using (var sequence = new Sequence(sequenceName, 1))
                {
                    Assert.Equal(2, sequence.GetNextValue());
                }

                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                {
                    return Task.Run(() =>
                    {
                        using (new Sequence(sequenceName, 5)) { }
                    });
                });
            }
            finally
            {
                var destroyMe = new Sequence(sequenceName);
                using (destroyMe)
                {
                }

                destroyMe.Destroy();
            }
        }

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
                using var sequence = new Sequence(sequenceName, startWith: startWith, incrementBy: incrementBy);
                Assert.Equal(afterIncrement, sequence.GetNextValue());
            }
            finally
            {
                var destroyMe = new Sequence(sequenceName);
                using (destroyMe)
                {
                }

                destroyMe.Destroy();
            }
        }
    }
}