// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Data;
using egregore.Extensions;

namespace egregore.Ontology
{
    public sealed class User : ILogSerialized
    {
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Uuid);
            context.bw.WriteNullableString(Name);
            context.bw.WriteNullableString(EmailAddress);
        }

        public User(LogDeserializeContext context)
        {
            Uuid = context.br.ReadGuid();
            Name = context.br.ReadNullableString();
            EmailAddress = context.br.ReadNullableString();
        }
    }
}