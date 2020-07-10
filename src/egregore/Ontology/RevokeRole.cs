// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Ontology
{
    public sealed class RevokeRole : Privilege
    {
        public RevokeRole(string role, byte[] grantor, byte[] grantee, byte[] signature = null) : base(Constants.Commands.RevokeRole, signature)
        {
            Value = role;
            Authority = grantor;
            Subject = grantee;
        }

        public RevokeRole(LogDeserializeContext context) : base(context)
        {

        }
    }
}