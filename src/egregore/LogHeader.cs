// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Extensions;

namespace egregore
{
    public struct LogHeader : ILogSerialized, ILogDescriptor
    {
        public ulong Version { get; set; }
        public byte[] PreviousHash { get; set; }
        public byte[] HashRoot { get; set; }
        public UInt128 Timestamp { get; set; }
        public byte[] Nonce { get; set; }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            Serialize(this, context, hash);
        }

        internal static void Serialize(ILogDescriptor descriptor, LogSerializeContext context, bool hash)
        {
            context.bw.Write(context.Version); // Version
            context.bw.WriteVarBuffer(descriptor.PreviousHash); // PreviousHash
            if (!hash)
                context.bw.WriteVarBuffer(descriptor.HashRoot); // HashRoot
            context.bw.Write(descriptor.Timestamp); // Timestamp
            context.bw.WriteVarBuffer(descriptor.Nonce); // Nonce
        }

        internal static void Deserialize(ILogDescriptor descriptor, LogDeserializeContext context)
        {
            descriptor.Version = context.br.ReadUInt64(); // Version
            descriptor.PreviousHash = context.br.ReadVarBuffer(); // PreviousHash
            descriptor.HashRoot = context.br.ReadVarBuffer(); // HashRoot
            descriptor.Timestamp = context.br.ReadUInt128(); // Timestamp
            descriptor.Nonce = context.br.ReadVarBuffer(); // Nonce
        }
    }
}