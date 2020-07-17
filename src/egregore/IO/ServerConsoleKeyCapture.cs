// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace egregore.IO
{
    internal sealed class ServerConsoleKeyCapture : IPersistedKeyCapture
    {
        private unsafe byte* _password;
        private int _passwordLength;

        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey(true);
        }

        public void Reset()
        {
            Console.Clear();
        }

        public void OnKeyRead(TextWriter @out)
        {
        }

        public unsafe void Sink(byte* password, int passwordLength)
        {
            _password = password;
            _passwordLength = passwordLength;
        }

        public unsafe bool TryReadPersisted(out byte* password, out int passwordLength)
        {
            if (_password == default)
            {
                password = default;
                passwordLength = default;
                return false;
            }

            password = _password;
            passwordLength = _passwordLength;
            return true;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            unsafe
            {
                NativeMethods.sodium_free(_password);
            }
        }

        ~ServerConsoleKeyCapture()
        {
            ReleaseUnmanagedResources();
        }
    }
}