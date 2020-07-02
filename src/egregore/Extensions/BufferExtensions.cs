// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace egregore.Extensions
{
    internal static class QueueExtensions
    {
        public static bool EndOfSubArguments(this Queue<string> arguments) => arguments.Count == 0 || arguments.Peek().StartsWith("-");
    }

    internal static class BufferExtensions
    {
        #region Sugar 

        public static bool WriteBoolean(this BinaryWriter bw, bool value)
        {
            bw.Write(value);
            return value;
        }

        #endregion

        #region Nullable<String>

        public static void WriteNullableString(this BinaryWriter bw, string value)
        {
            if (bw.WriteBoolean(value != null))
                bw.Write(value);
        }

        public static string ReadNullableString(this BinaryReader br)
        {
            return br.ReadBoolean() ? br.ReadString() : null;
        }

        #endregion
        
        #region Nullable<UInt64>

        public static void WriteNullableUInt64(this BinaryWriter bw, ulong? value)
        {
            if (bw.WriteBoolean(value.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                bw.Write(value.Value);
        }

        public static ulong? ReadNullableUInt64(this BinaryReader br)
        {
            return br.ReadBoolean() ? (ulong?) br.ReadUInt64() : null;
        }

        #endregion
        
        #region UInt128

        public static void Write(this BinaryWriter bw, UInt128 value)
        {
            bw.Write(value.v1);
            bw.Write(value.v2);
        }

        public static UInt128 ReadUInt128(this BinaryReader br)
        {
            var v1 = br.ReadUInt64();
            var v2 = br.ReadUInt64();
            return new UInt128(v1, v2);
        }

        #endregion
        
        #region VarBuffer

        public static void WriteVarBuffer(this BinaryWriter bw, byte[] buffer)
        {
            var hasBuffer = buffer != null;
            if (!bw.WriteBoolean(hasBuffer) || !hasBuffer)
                return;
            bw.Write(buffer.Length);
            bw.Write(buffer);
        }

        public static byte[] ReadVarBuffer(this BinaryReader br)
        {
            if (!br.ReadBoolean())
                return null;
            var length = br.ReadInt32();
            var buffer = br.ReadBytes(length);
            return buffer;
        }

        #endregion
    }
}