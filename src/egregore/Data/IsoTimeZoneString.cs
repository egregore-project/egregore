// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace egregore.Data
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public struct IsoTimeZoneString : IEquatable<IsoTimeZoneString>
    {
        public DateTimeOffset Timestamp { get; }
        public string TimeZone { get; }

        public IsoTimeZoneString(string timestamp)
        {
            if(string.IsNullOrWhiteSpace(timestamp))
                throw new FormatException("missing timestamp");

            var tokens = timestamp.Split('[');
            if(tokens.Length != 2)
                throw new FormatException("invalid timestamp");

            if(!DateTimeOffset.TryParse(tokens[0], out var dto))
                throw new FormatException("invalid timestamp");

            Timestamp = dto;

            var tz = tokens[1];
            if(string.IsNullOrWhiteSpace(tz) || tz.Length < 3)
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

        public static bool operator ==(IsoTimeZoneString left, IsoTimeZoneString right) => left.Equals(right);

        public static bool operator !=(IsoTimeZoneString left, IsoTimeZoneString right) => !left.Equals(right);

        public override string ToString() => $"{Timestamp:o}[{TimeZone}]";
    }
}