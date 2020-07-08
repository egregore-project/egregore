// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace egregore.Tests
{
    internal sealed class TempKeyFileService : IKeyFileService
    {
        private readonly string _filePath;
        private readonly FileStream _fileStream;

        public TempKeyFileService()
        {
            _filePath = Path.GetTempFileName();
            _fileStream = File.Open(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        public string GetKeyFilePath() => _filePath;

        public FileStream GetKeyFileStream()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
            return _fileStream;
        }

        public unsafe byte* GetSecretKeyPointer(IKeyCapture capture, [CallerMemberName] string callerMemberName = null) => Crypto.LoadSecretKeyPointerFromFileStream(GetKeyFilePath(), GetKeyFileStream(), capture, callerMemberName);

        public void Dispose()
        {
            try
            {
                _fileStream.Dispose();
                File.Delete(_filePath);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
    }
}