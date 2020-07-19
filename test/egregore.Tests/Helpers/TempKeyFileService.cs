// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace egregore.Tests.Helpers
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

        public FileStream GetKeyFileStream()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
            return _fileStream;
        }

        public unsafe byte* GetSecretKeyPointer(IKeyCapture capture, [CallerMemberName] string callerMemberName = null)
        {
            return Crypto.LoadSecretKeyPointerFromFileStream(GetKeyFilePath(), GetKeyFileStream(), capture,
                callerMemberName);
        }

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

        public string GetKeyFilePath()
        {
            return _filePath;
        }
    }
}