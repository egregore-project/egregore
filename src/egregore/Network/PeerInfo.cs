// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace egregore.Network
{
    public struct PeerInfo : IEquatable<PeerInfo>, IComparable<PeerInfo>, IComparable
    {
        public string Name { get; }
        public int Port { get; }
        public string Address { get; }
        public string HostName { get; }

        public PeerInfo(string name, int port)
        {
            Name = name;
            Port = port;
            Address = $"tcp://{name}:{port}";
            HostName = Dns.GetHostEntry(name).HostName;
        }

        public override bool Equals(object obj)
        {
            return obj is PeerInfo other && Equals(other);
        }

        public override string ToString()
        {
            return Address;
        }

        #region Equality

        public bool Equals(PeerInfo other)
        {
            return Name == other.Name && Port == other.Port && Address == other.Address && HostName == other.HostName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Port, Address, HostName);
        }

        public static bool operator ==(PeerInfo left, PeerInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PeerInfo left, PeerInfo right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Comparison

        public int CompareTo(PeerInfo other)
        {
            var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;
            var portComparison = Port.CompareTo(other.Port);
            if (portComparison != 0) return portComparison;
            var addressComparison = string.Compare(Address, other.Address, StringComparison.Ordinal);
            if (addressComparison != 0) return addressComparison;
            return string.Compare(HostName, other.HostName, StringComparison.Ordinal);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is PeerInfo other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PeerInfo)}");
        }

        public static bool operator <(PeerInfo left, PeerInfo right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(PeerInfo left, PeerInfo right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(PeerInfo left, PeerInfo right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(PeerInfo left, PeerInfo right)
        {
            return left.CompareTo(right) >= 0;
        }

        #endregion
    }
}