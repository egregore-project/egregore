// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Data;
using Xunit;

namespace egregore.Tests.Data
{
    public class IsoTimeZoneStringTests
    {
        [Fact]
        public void Can_round_trip_time_zone()
        {
            var now = DateTimeOffset.Now;
            var timeZone = TimeZoneLookup.Now.TimeZone;
            
            var one = new IsoTimeZoneString(now, timeZone);
            Assert.Equal(now, one.Timestamp);
            Assert.Equal(timeZone, one.TimeZone);

            var two = new IsoTimeZoneString(one.ToString());
            Assert.Equal(one, two);
        }
    }
}