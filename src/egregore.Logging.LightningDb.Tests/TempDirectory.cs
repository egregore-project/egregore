// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;

namespace egregore.Logging.LightningDb.Tests
{
    public class TempDirectory : IDisposable
    {
        private readonly string _directory;

        public TempDirectory()
        {
            _directory = Path.Combine(Directory.GetCurrentDirectory(), "lmdb");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        public string NewDirectory()
        {
            var path = Path.Combine(_directory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            return path;
        }
    }
}