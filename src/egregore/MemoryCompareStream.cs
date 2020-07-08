// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace egregore
{
    internal sealed class MemoryCompareStream : Stream
    {
        private readonly byte[] _compareTo;

        public MemoryCompareStream(byte[] compareTo)
        {
            _compareTo = compareTo;
            Position = 0;
        }

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _compareTo.Length;

        public override long Position { get; set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
                if (buffer[offset + i] != _compareTo[Position + i])
                {
                    Debug.Assert(false);
                    throw new Exception("Data mismatch");
                }

            Position += count;
        }

        public override void WriteByte(byte value)
        {
            if (_compareTo[Position] != value)
            {
                Debug.Assert(false);
                throw new Exception("Data mismatch");
            }

            Position++;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _compareTo.Length - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }
    }
}