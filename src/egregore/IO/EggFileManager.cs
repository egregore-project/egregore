// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using egregore.Extensions;
using egregore.Ontology;
using Microsoft.Data.Sqlite;

namespace egregore.IO
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

            Monitor.Enter(Lock);
            try
            {
                var store = new LightningLogStore(eggPath);
                store.Init();
                Console.Out.WriteLine("Created egg file '{0}'", Path.GetFileName(eggPath));
                return true;
            }
            catch (SqliteException e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? $"Failed to create egg file at '{eggPath}'. SQLite must run on a volume with nobrl enabled. Use the '{Constants.EnvVars.EggFilePath}' environment variable to specify a compatible storage path. {e}"
                        : $"Failed to create egg file at '{eggPath}': {e}");

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