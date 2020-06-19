// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Schema
{
    public sealed class Namespace : ILogSerialized
    {
        public const ulong Type = 1UL;
        public const ulong Version = 1UL;

        public Namespace(string value)
        {
            Value = value;
        }

        public Namespace(LogDeserializeContext context)
        {
            Value = context.br.ReadString();
        }

        public string Value { get; set; }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Value);
        }
    }
}