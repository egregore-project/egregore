// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore.Ontology
{
    public sealed class GrantRole : Privilege
    {
        public GrantRole(string role, byte[] grantor, byte[] grantee) : base("grant_role")
        {
            Value = role;
            Authority = grantor;
            Subject = grantee;
        }

        public GrantRole(string role, byte[] grantor, byte[] grantee, byte[] signature) : base("grant_role", signature)
        {
            Value = role;
            Authority = grantor;
            Subject = grantee;
        }

        public GrantRole(LogDeserializeContext context) : base(context) { }
    }
}