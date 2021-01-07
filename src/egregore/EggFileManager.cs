// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using egregore.Data;
using egregore.Extensions;
using LightningDB;

namespace egregore
{
    internal static class EggFileManager
    {
        private static readonly object Lock = new object();

        public static bool Create(string eggPath)
        {
            if (Path.GetFileName(eggPath).IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteErrorLine(Strings.InvalidCharactersInPath);
                return false;
            }

            try
            {
                eggPath = Path.GetFullPath(eggPath);
                if (!Path.HasExtension(eggPath))
                    eggPath = Path.ChangeExtension(eggPath, ".egg");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine("Invalid egg file path");
                return false;
            }

            if (File.Exists(eggPath))
            {
                Console.Error.WriteErrorLine(
                    "Egg file already exists at this path. For safety, you must manually remove it before generating a new egg with this path.");
                return false;
            }

            Monitor.Enter(Lock);
            try
            {
                var store = new LightningLogStore(new LogObjectTypeProvider());
                store.Init(eggPath);
                Console.Out.WriteLine("Created egg file '{0}'", Path.GetFileName(eggPath));
                return true;
            }
            catch (LightningException e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine($"Failed to create egg file at '{eggPath}': {e}");
                return false;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine("Failed to create egg file at '{0}': {1}", eggPath, e);
                return false;
            }
            finally
            {
                Monitor.Exit(Lock);
            }
        }
    }
}