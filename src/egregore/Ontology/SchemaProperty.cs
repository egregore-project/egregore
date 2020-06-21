// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace egregore.Ontology
{
    public class SchemaProperty : ILogSerialized
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public SchemaProperty() { }
        
        #region Serialization
        
        public SchemaProperty(LogDeserializeContext context)
        {
            Name = context.br.ReadString();
            Type = context.br.ReadString();
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Name);
            context.bw.Write(Type);
        }

        #endregion
    }
}