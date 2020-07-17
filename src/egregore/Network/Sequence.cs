// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace egregore.Network
{
    /// <summary> Provides an atomic, incrementing value that's globally scoped and persisted to disk. </summary>
    public class Sequence : IDisposable
    {
        private readonly long _incrementBy;
        private readonly string _name;
        private readonly long _startWith;

        public Sequence(string name, long startWith = -1, long incrementBy = 1)
        {
            _name = name;
            _startWith = startWith;
            _incrementBy = incrementBy;

            if (TryAcquireOutOfProcessLock(out var mutex))
                try
                {
                    if (!File.Exists(SequenceName))
                        CreateNewMapSource(SequenceName);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

            var current = Current;
            if (current < _startWith)
                throw new ArgumentOutOfRangeException(
                    $"You cannot change starting value to '{_startWith}' for an existing sequence whose current value is {current}");
        }

        public long Current => GetCurrentValue();

        private string SequenceName => $@"egregore_sequence_{_name}";
        private string MutexName => $@"egregore_mutex_{_name}";

        public void Dispose()
        {
        }

        private static MemoryMappedFile OpenExistingMapSource(string mapName)
        {
            return MemoryMappedFile.CreateFromFile(mapName, FileMode.Open, mapName, sizeof(long));
        }

        private void CreateNewMapSource(string mapName)
        {
            using var fs = new FileStream(mapName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var bw = new BinaryWriter(fs);
            fs.SetLength(sizeof(long));
            bw.Write(_startWith);
        }

        private long GetCurrentValue()
        {
            if (!TryAcquireOutOfProcessLock(out var mutex))
                return -1;

            try
            {
                using var mmf = OpenExistingMapSource(SequenceName);
                using var vs = mmf.CreateViewStream();
                long current;
                using (var reader = new BinaryReader(vs))
                {
                    current = reader.ReadInt64();
                }

                return current;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private bool TryAcquireOutOfProcessLock(out Mutex mutex)
        {
            try
            {
                mutex = new Mutex(true, MutexName, out var acquired);
                if (!acquired)
                    mutex.WaitOne(TimeSpan.FromSeconds(10));
                return true;
            }
            catch (AbandonedMutexException e)
            {
                // This is a serious programming error, because it basically means our code failed to release a mutex gracefully last time around.
                // This is thrown because we have just acquired an abandoned mutex, which was not released properly.
                // We could try to recover, but since we own this lock exclusively, it actually means there's a problem we should fix.
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        public long GetNextValue()
        {
            if (!TryAcquireOutOfProcessLock(out var mutex))
                return -1;

            try
            {
                long current;

                using (var mmf = OpenExistingMapSource(SequenceName))
                {
                    using (var vs = mmf.CreateViewStream())
                    {
                        using var reader = new BinaryReader(vs);
                        current = reader.ReadInt64();
                    }

                    using (var vs = mmf.CreateViewStream())
                    {
                        using var bw = new BinaryWriter(vs);
                        current += _incrementBy;
                        bw.Write(current);
                    }
                }

                return current;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void Destroy()
        {
            if (!TryAcquireOutOfProcessLock(out var mutex))
                return;

            try
            {
                File.Delete(SequenceName);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}