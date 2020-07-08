// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace egregore.IO
{
    internal sealed class ServerConsoleKeyCapture : IPersistedKeyCapture
    {
        private unsafe byte* _password;
        private int _passwordLength;

        public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);
        public void Reset() => Console.Clear();
        public void OnKeyRead(TextWriter @out) { }

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

        private void ReleaseUnmanagedResources()
        {
            unsafe
            {
                NativeMethods.sodium_free(_password);
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ServerConsoleKeyCapture()
        {
            ReleaseUnmanagedResources();
        }
    }
}