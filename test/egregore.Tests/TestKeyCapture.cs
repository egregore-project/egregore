// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace egregore.Tests
{
    internal sealed class TestKeyCapture : IKeyCapture
    {
        private const char EnterKeyChar = '\u0000';
        internal static readonly ConsoleKeyInfo EnterKey = new ConsoleKeyInfo(EnterKeyChar, ConsoleKey.Enter, false, false, false);

        private readonly string _value;
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
            _index = 0;
        }

        public ConsoleKeyInfo ReadKey()
        {
            if (_index == _value.Length)
                return EnterKey;

            var keyChar = _value[_index++];
            if (keyChar == EnterKeyChar)
                return EnterKey;

            Enum.TryParse<ConsoleKey>(keyChar.ToString().ToUpper(), out var consoleKey);
            return new ConsoleKeyInfo(keyChar, consoleKey, false, false, false);
        }
    }
}