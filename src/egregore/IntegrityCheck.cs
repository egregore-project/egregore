// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace egregore
{
    internal sealed class IntegrityCheck
    {
        private const string RuntimesPathSegment = "runtimes";

        public static IntPtr Preload(string libraryName, Assembly assembly, DllImportSearchPath? searchPath = null)
        {
            // https://libsodium.gitbook.io/doc/installation#integrity-checking
            const string publicKeyString = "RWQf6LRCGA9i53mlYecO4IzT51TGPpvWucNSCh1CBM0QTaLn73Y7GFO3";
            return VerifyAndLoad(assembly, libraryName, NativeMethods.DllName, publicKeyString, searchPath);
        }

        private static IntPtr VerifyAndLoad(Assembly assembly, string source, string target, string publicKey,
            DllImportSearchPath? searchPath)
        {
            var handle = IntPtr.Zero;
            if (source == target)
            {
                var architecture = Environment.Is64BitProcess ? "x64" : "x86";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var libraryFileName = $"{target}.dylib";
                    var fullPath = Path.Combine(RuntimesPathSegment, $"osx-{architecture}", "native", libraryFileName);
                    IntegrityCheckFile(fullPath, publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var libraryFileName = $"{target}.so";
                    var fullPath = Path.Combine(RuntimesPathSegment, $"linux-{architecture}", "native",
                        libraryFileName);
                    IntegrityCheckFile(fullPath, publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
                else
                {
                    var libraryFileName = $"{target}.dll";
                    var fullPath = Path.Combine(RuntimesPathSegment, $"win-{architecture}", "native", libraryFileName);
                    IntegrityCheckFile(fullPath, publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
            }

            return handle; // IntPtr.Zero means fallback to platform loading strategy
        }

        private static void IntegrityCheckFile(string fullPath, string publicKeyString)
        {
            Debug.WriteLine($"Resolving libsodium location to: {fullPath}");
        }
    }
}