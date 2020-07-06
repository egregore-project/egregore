// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore.Ontology
{
    public sealed class RevokeRole : Privilege
    {
        public RevokeRole(string role, byte[] grantor, byte[] grantee) : base("revoke_role")
        {
            Value = role;
            Authority = grantor;
            Subject = grantee;
        }

        public RevokeRole(string role, byte[] grantor, byte[] grantee, byte[] signature) : base("revoke_role", signature)
        {
            Value = role;
            Authority = grantor;
            Subject = grantee;
        }

        public RevokeRole(LogDeserializeContext context) : base(context) { }
    }
}