// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Runtime.CompilerServices;
using egregore.Cryptography;

namespace egregore.Ontology
{
    public sealed class ServerKeyFileService : IKeyFileService
    {
        public FileStream GetKeyFileStream()
        {
            return Program.keyFileStream;
        }

        public unsafe byte* GetSecretKeyPointer(IKeyCapture capture, [CallerMemberName] string callerMemberName = null)
        {
            var keyFilePath = GetKeyFilePath();
            var keyFileStream = GetKeyFileStream();

            var ptr = capture is IPersistedKeyCapture persisted
                ? Crypto.LoadSecretKeyPointerFromFileStream(keyFilePath, keyFileStream, persisted, callerMemberName)
                : Crypto.LoadSecretKeyPointerFromFileStream(keyFilePath, keyFileStream, capture, callerMemberName);

            return ptr;
        }

        public void Dispose()
        {
        }

        public string GetKeyFilePath()
        {
            return Program.keyFilePath;
        }
    }
}