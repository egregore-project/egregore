// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Extensions;

namespace egregore.Ontology
{
    public sealed class Page : ILogSerialized
    {
        public Page()
        {
        }

        public Page(LogDeserializeContext context)
        {
            Uuid = context.br.ReadGuid();
            Title = context.br.ReadNullableString();
            Body = context.br.ReadNullableString();
            BodyPlainText = context.br.ReadNullableString();
            BodyHtml = context.br.ReadNullableString();
        }

        public Guid Uuid { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string BodyPlainText { get; set; }
        public string BodyHtml { get; set; }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.Write(Uuid);
            context.bw.WriteNullableString(Title);
            context.bw.WriteNullableString(Body);
            context.bw.WriteNullableString(BodyPlainText);
            context.bw.WriteNullableString(BodyHtml);
        }
    }
}