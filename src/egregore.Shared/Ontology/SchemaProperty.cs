// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace egregore.Ontology
{
    public class SchemaProperty : ILogSerialized
    {
        public SchemaProperty() { }

        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }

        #region Serialization

        public SchemaProperty(LogDeserializeContext context)
        {
            Name = context.br.ReadString();
            Type = context.br.ReadString();

            IsRequired = context.br.ReadBoolean();
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Name);
            context.bw.Write(Type);

            context.bw.Write(IsRequired);
        }

        #endregion
    }
}