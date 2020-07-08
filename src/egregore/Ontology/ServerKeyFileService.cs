// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.CompilerServices;

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