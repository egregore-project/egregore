// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using egregore.Ontology;
using Xunit;

namespace egregore.Tests
{
    public class RoleAccessTests
    {
        [Fact]
        public void Can_grant_and_assign_roles_between_users()
        {
            var rootPubKey = CryptoTests.GenerateSecretKeyOnDisk(out var rootFile);
            var userPubKey = CryptoTests.GenerateSecretKeyOnDisk(out var userFile);
            
            var grant = new GrantRole("admin", rootPubKey, userPubKey);
            grant.Sign(File.OpenRead(rootFile));
            Assert.True(grant.Verify());

            var revoke = new RevokeRole("admin", rootPubKey, userPubKey);
            revoke.Sign(File.OpenRead(rootFile));
            Assert.True(revoke.Verify());
        }
    }
}