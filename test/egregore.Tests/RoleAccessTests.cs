// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Ontology;
using Xunit;

namespace egregore.Tests
{
    public class RoleAccessTests
    {
        [Fact]
        public void Can_grant_and_assign_roles_between_users()
        {
            var root = Crypto.GenerateKeyPair();
            var user = Crypto.GenerateKeyPair();

            var grant = new GrantRole("admin", root.publicKey, user.publicKey);
            grant.Sign(root.secretKey);
            Assert.True(grant.Verify());

            var revoke = new RevokeRole("admin", root.publicKey, user.publicKey);
            revoke.Sign(root.secretKey);
            Assert.True(revoke.Verify());
        }
    }
}