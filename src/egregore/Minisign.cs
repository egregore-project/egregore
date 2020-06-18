// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using Minisign;

namespace egregore
{
    internal sealed class Minisign
    {
        public static IntPtr VerifyImportResolver(string libraryName, Assembly assembly,
            DllImportSearchPath? searchPath)
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
                    IntegrityCheckFile(Path.Combine("runtimes", $"osx-{architecture}", "native", libraryFileName),
                        publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var libraryFileName = $"{target}.so";
                    IntegrityCheckFile(Path.Combine("runtimes", $"linux-{architecture}", "native", libraryFileName),
                        publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
                else
                {
                    var libraryFileName = $"{target}.dll";
                    IntegrityCheckFile(Path.Combine("runtimes", $"win-{architecture}", "native", libraryFileName),
                        publicKey);
                    NativeLibrary.TryLoad(libraryFileName, assembly, searchPath, out handle);
                }
            }

            return handle; // IntPtr.Zero means fallback to platform loading strategy
        }

        private static void IntegrityCheckFile(string fullPath, string publicKeyString)
        {
            return; // FIXME: https://github.com/jedisct1/libsodium/issues/971

            var resolver = new AssemblyDependencyResolver(AppDomain.CurrentDomain.BaseDirectory);
            var libraryPath = resolver.ResolveUnmanagedDllToPath(fullPath);
            if (!File.Exists(libraryPath))
                return;

            var publicKey = Core.LoadPublicKeyFromString(publicKeyString);
            var signature = Core.LoadSignatureFromFile(libraryPath);

            if (!Core.ValidateSignature(libraryPath, signature, publicKey))
                throw new SecurityException("library has been tampered with!");
        }
    }
}