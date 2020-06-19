// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace egregore
{
    internal sealed class MemoryCompareStream : Stream
    {
        public MemoryCompareStream(byte[] compareTo)
        {
            _compareTo = compareTo;
            _position = 0;
        }

        readonly byte[] _compareTo;
        long _position;

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (buffer[offset + i] != _compareTo[_position + i])
                {

                    Debug.Assert(false);
                    throw new Exception("Data mismatch");
                }
            }

            _position += count;
        }

        public override void WriteByte(byte value)
        {
            if (_compareTo[_position] != value)
            {
                Debug.Assert(false);
                throw new Exception("Data mismatch");
            }

            _position++;
        }

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override void Flush() { }
        public override long Length => _compareTo.Length;

        public override long Position
        { 
            get => _position;
            set => _position = value;
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
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _compareTo.Length - offset;
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