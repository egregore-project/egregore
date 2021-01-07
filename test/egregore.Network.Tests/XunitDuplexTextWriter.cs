// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace egregore.Network.Tests
{
    internal sealed class XunitDuplexTextWriter : TextWriter
    {
        private readonly StringBuilder _buffer;
        private readonly TextWriter _inner;
        private readonly ITestOutputHelper _output;

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