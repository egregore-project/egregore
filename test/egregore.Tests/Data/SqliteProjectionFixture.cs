// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using egregore.Data;

namespace egregore.Tests.Data
{
    public sealed class SqliteProjectionFixture : IDisposable
    {
        public SqliteProjectionFixture()
        {
            var projection = new SqliteProjection($"{Guid.NewGuid()}.db");
            projection.Init();
            Projection = projection;
        }

        public SqliteProjection Projection { get; }

        public void Dispose()
        {
            var dataFile = Projection?.DataFile;
            if (string.IsNullOrWhiteSpace(dataFile))
                return;

            try
            {
                File.Delete(dataFile);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}