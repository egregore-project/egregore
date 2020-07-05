// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using egregore.Ontology;
using Xunit;

namespace egregore.Tests
{
    internal sealed class TestKeyFileService : IKeyFileService, IDisposable
    {
        private readonly string _filePath;
        private readonly FileStream _fileStream;

        public TestKeyFileService()
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