// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace egregore.Tests
{
    internal sealed class XunitDuplexTextWriter : TextWriter
    {
        private readonly ITestOutputHelper _output;
        private readonly TextWriter _inner;
        private readonly StringBuilder _buffer;

        public XunitDuplexTextWriter(ITestOutputHelper output, TextWriter inner)
        {
            _output = output;
            _inner = inner;
            _buffer = new StringBuilder();
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _buffer.Append(value);
            _inner.Write(value);
        }

        public override void Write(string value)
        {
            _buffer.Append(value);
            _inner.Write(value);
        }

        public override void WriteLine()
        {
            _output.WriteLine(_buffer.ToString());
            _buffer.Clear();
            _inner.WriteLine();
        }

        public override void WriteLine(string value)
        {
            _buffer.Append(value);
            _output.WriteLine(_buffer.ToString());
            _buffer.Clear();
            _inner.WriteLine(value);
        }
    }
}