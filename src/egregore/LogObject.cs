// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Extensions;

namespace egregore
{
    public sealed class LogObject : ILogSerialized
    {
        internal LogObject() { }

        public int Index { get; set; }
        public ulong? Type { get; set; }
        public ulong Version { get; set; }
        public ILogSerialized Data { get; set; }
        public UInt128 Timestamp { get; set; }
        public byte[] Hash { get; internal set; }

        #region Serialization
        
        public void Serialize(LogSerializeContext context, bool hash)
        {
            Type = context.typeProvider.Get(Data?.GetType());

            context.bw.Write(Index);                 // Index
            context.bw.WriteNullableUInt64(Type);	 // Type
            context.bw.Write(Version);               // Version
            context.bw.Write(Timestamp);             // Timestamp
            if(!hash)
                context.bw.WriteVarBuffer(Hash);     // Hash

            if (!context.bw.WriteBoolean(Data != null) || !Type.HasValue)
                return;
            Data?.Serialize(context, hash);
        }

        internal LogObject(LogDeserializeContext context)
        {
            Index = context.br.ReadInt32();             // Index
            Type = context.br.ReadNullableUInt64();		// Type
            Version = context.br.ReadUInt64();			// Version
            Timestamp = context.br.ReadUInt128();		// Timestamp
            Hash = context.br.ReadVarBuffer();          // Hash

            if (!context.br.ReadBoolean() || !Type.HasValue)
                return;
            var type = context.typeProvider.Get(Type.Value);
            if (type != null)
                Data = context.typeProvider.Deserialize(type, context);
        }

        #endregion
    }
}