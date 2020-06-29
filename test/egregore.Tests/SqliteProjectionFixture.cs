// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using egregore.Data;

namespace egregore.Tests
{
    public sealed class SqliteProjectionFixture : IDisposable
    {
        public SqliteProjection Projection { get; }

        public SqliteProjectionFixture()
        {
            var projection = new SqliteProjection($"{Guid.NewGuid()}.db");
            projection.Init();
            Projection = projection;
        }

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