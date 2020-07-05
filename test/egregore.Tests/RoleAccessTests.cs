// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Ontology;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class RoleAccessTests
    {
        private readonly ITestOutputHelper _output;

        public RoleAccessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Can_grant_and_assign_roles_between_users()
        {
            var capture = new TestKeyCapture("rosebud", "rosebud");
            
            var rootPubKey = CryptoTestHarness.GenerateSecretKeyOnDisk(_output, capture, out var rootFile);
            capture.Reset();

            var userPubKey = CryptoTestHarness.GenerateSecretKeyOnDisk(_output, capture, out _);
            capture.Reset();

            var grant = new GrantRole("admin", rootPubKey, userPubKey);
            grant.Sign(rootFile, capture);
            Assert.True(grant.Verify());

            capture.Reset();

            var revoke = new RevokeRole("admin", rootPubKey, userPubKey);
            revoke.Sign(rootFile, capture);
            Assert.True(revoke.Verify());
        }
    }
}