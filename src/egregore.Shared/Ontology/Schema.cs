// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace egregore.Ontology
{
    public sealed class Schema : ILogSerialized
    {
        public Schema()
        {
        }

        public string Name { get; set; }
        public List<SchemaProperty> Properties { get; set; } = new List<SchemaProperty>();

        #region Serialization

        public Schema(LogDeserializeContext context)
        {
            Name = context.br.ReadString();
            DeserializeProperties(context);
        }

        private void DeserializeProperties(LogDeserializeContext context)
        {
            var count = context.br.ReadInt32();
            Properties = new List<SchemaProperty>(count);
            for (var i = 0; i < count; i++)
                Properties.Add(new SchemaProperty(context));
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Name);
            SerializeProperties(context, hash);
        }

        private void SerializeProperties(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Properties.Count);
            foreach (var property in Properties)
                property.Serialize(context, hash);
        }

        #endregion
    }
}