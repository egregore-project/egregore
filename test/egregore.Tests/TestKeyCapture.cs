// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace egregore.Tests
{
    internal sealed class TestKeyCapture : IKeyCapture
    {
        private const char EnterKeyChar = '\u0000';

        private readonly string _value;
        private int _iterations;

        private int _index;

        public TestKeyCapture(params string[] values)
        {
            var sb = new StringBuilder();
            foreach (var value in values)
            {
                sb.Append(value);
                sb.Append(EnterKeyChar);
            }
            _value = sb.ToString();
            _iterations = 1;
            _index = 0;
        }

        public TestKeyCapture(string value, int iterations)
        {
            _value = value;
            _iterations = iterations;
            _index = 0;
        }

        public ConsoleKeyInfo ReadKey()
        {
            if (_index == _value.Length)
            {
                _iterations--;
                if (_iterations > 2)
                    _index = 0;
                return new ConsoleKeyInfo(EnterKeyChar, ConsoleKey.Enter, false, false, false);
            }

            var keyChar = _value[_index++];
            if(keyChar == EnterKeyChar)
                return new ConsoleKeyInfo(keyChar, ConsoleKey.Enter, false, false, false);

            Enum.TryParse<ConsoleKey>(keyChar.ToString().ToUpper(), out var consoleKey);
            return new ConsoleKeyInfo(keyChar, consoleKey, false, false, false);
        }
    }
}