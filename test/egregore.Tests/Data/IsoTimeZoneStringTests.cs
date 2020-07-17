// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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