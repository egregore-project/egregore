// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Ontology
{
    public sealed class Namespace : ILogSerialized
    {
        public Namespace(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        #region Serialization

        public Namespace(LogDeserializeContext context)
        {
            Value = context.br.ReadString();
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Value);
        }

        #endregion
    }
}