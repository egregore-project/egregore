// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace egregore
{
    internal sealed class HashProvider
    {
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly HashAlgorithm _algorithm;

        public HashProvider(ILogObjectTypeProvider typeProvider) : this(typeProvider, SHA256.Create()) { }
        internal HashProvider(ILogObjectTypeProvider typeProvider, HashAlgorithm algorithm)
        {
            _typeProvider = typeProvider;
            _algorithm = algorithm;
        }

        public byte[] ComputeHashBytes(LogEntry entry)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            entry.Serialize(new LogSerializeContext(bw, _typeProvider), true);
            return _algorithm.ComputeHash(ms);
        }

        public byte[] ComputeHashBytes(ILogSerialized data)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            data.Serialize(new LogSerializeContext(bw, _typeProvider), true);
            return _algorithm.ComputeHash(ms);
        }

        private static readonly LogObject NoLogObjects = new LogObject();

        public byte[] ComputeHashRootBytes(LogEntry entry)
        {
            // https://en.bitcoin.it/wiki/Protocol_documentation#Merkle_Trees

            if (entry.Objects == null || entry.Objects.Count == 0)
                return ComputeHashBytes(NoLogObjects);

            var p = new List<byte[]>();
            foreach (var o in entry.Objects)
                p.Add(_algorithm.ComputeHash(ComputeHashBytes(o)));

            if(p.Count > 1 && p.Count % 2 != 0)
                p.Add(p[^1]);
            if (p.Count == 1)
                return p[0];

            pass:
            {
                var n = new List<byte[]>(p.Count / 2);
                for (var i = 0; i < p.Count; i++)
                {
                    for (var j = i + 1; j < p.Count; j++)
                    {
                        var a = entry.Objects[i].Hash;
                        var b = entry.Objects[j].Hash;
                        var d = DoubleHash(a, b);
                        n.Add(d);
                        i++;
                    }
                }
                if (n.Count == 1)
                    return n[0];
                if (n.Count % 2 != 0)
                    n.Add(n[^1]);

                p = n;
                goto pass;
            }
        }
        
        private byte[] DoubleHash(byte[] a, byte[] b)
        {
            var buffer = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, buffer, 0, a.Length);
            Buffer.BlockCopy(b, 0, buffer, buffer.Length, b.Length);

            var one = _algorithm.ComputeHash(buffer);
            var two = _algorithm.ComputeHash(one);

            return two;
        }
    }
}