// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Data;

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