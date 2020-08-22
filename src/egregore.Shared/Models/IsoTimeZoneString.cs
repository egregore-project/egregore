// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;

namespace egregore.Models
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public struct IsoTimeZoneString : IEquatable<IsoTimeZoneString>
    {
        public DateTimeOffset Timestamp { get; }
        public string TimeZone { get; }

        public IsoTimeZoneString(string timestamp)
        {
            if (string.IsNullOrWhiteSpace(timestamp))
                throw new FormatException("missing timestamp");

            var tokens = timestamp.Split('[');
            if (tokens.Length != 2)
                throw new FormatException("invalid timestamp");

            if (!DateTimeOffset.TryParse(tokens[0], out var dto))
                throw new FormatException("invalid timestamp");

            Timestamp = dto;

            var tz = tokens[1];
            if (string.IsNullOrWhiteSpace(tz) || tz.Length < 3)
                throw new FormatException("invalid time zone");

            TimeZone = tz.Substring(0, tz.Length - 1);
        }

        public IsoTimeZoneString(DateTimeOffset timestamp, string timeZone)
        {
            Timestamp = timestamp;
            TimeZone = timeZone;
        }

        public bool Equals(IsoTimeZoneString other)
        {
            return Timestamp.Equals(other.Timestamp) &&
                   string.Equals(TimeZone, other.TimeZone, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is IsoTimeZoneString other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Timestamp);
            hashCode.Add(TimeZone, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(IsoTimeZoneString left, IsoTimeZoneString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IsoTimeZoneString left, IsoTimeZoneString right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Timestamp:o}[{TimeZone}]";
        }
    }
}