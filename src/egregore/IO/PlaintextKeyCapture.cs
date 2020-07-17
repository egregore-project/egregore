// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;

namespace egregore.IO
{
    internal sealed class PlaintextKeyCapture : IKeyCapture
    {
        private const char EnterKeyChar = '\u0000';
        private const char BackspaceKeyChar = '\b';

        internal static readonly ConsoleKeyInfo EnterKey =
            new ConsoleKeyInfo(EnterKeyChar, ConsoleKey.Enter, false, false, false);

        internal static readonly ConsoleKeyInfo BackspaceKey =
            new ConsoleKeyInfo(BackspaceKeyChar, ConsoleKey.Backspace, false, false, false);

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

        public void OnKeyRead(TextWriter @out)
        {
        }
    }
}