// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace egregore
{
    internal sealed class PlaintextKeyCapture : IKeyCapture
    {
        private const char EnterKeyChar = '\u0000';
        private const char BackspaceKeyChar = '\b';

        internal static readonly ConsoleKeyInfo EnterKey = new ConsoleKeyInfo(EnterKeyChar, ConsoleKey.Enter, false, false, false);
        internal static readonly ConsoleKeyInfo BackspaceKey = new ConsoleKeyInfo(BackspaceKeyChar, ConsoleKey.Backspace, false, false, false);

        private readonly string _value;
        private int _index;

        public PlaintextKeyCapture(params string[] values)
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
            
            switch (keyChar)
            {
                case EnterKeyChar:
                    return EnterKey;
                case BackspaceKeyChar:
                    return BackspaceKey;
                default:
                    Enum.TryParse<ConsoleKey>(keyChar.ToString().ToUpper(), out var consoleKey);
                    return new ConsoleKeyInfo(keyChar, consoleKey, false, false, false);
            }
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}