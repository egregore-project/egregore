// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using egregore.IO;
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
        public void Can_grant_and_revoke_roles_between_users()
        {
            var capture = new PlaintextKeyCapture("rosebud", "rosebud");
            var service = new TestKeyFileService();
            
            var rootPubKey = CryptoTestHarness.GenerateKeyFile(_output, capture, service);
            capture.Reset();

            var userPubKey = CryptoTestHarness.GenerateKeyFile(_output, capture, new TestKeyFileService());
            capture.Reset();

            var grant = new GrantRole("admin", rootPubKey, userPubKey);
            grant.Sign(service, capture);

            Assert.True(grant.Authority.SequenceEqual(rootPubKey));
            Assert.True(grant.Subject.SequenceEqual(userPubKey));
            Assert.True(grant.Verify(), "grant was not verified");

            capture.Reset();
            var revoke = new RevokeRole(Constants.OwnerRole, rootPubKey, userPubKey);
            revoke.Sign(service, capture);

            Assert.True(revoke.Authority.SequenceEqual(rootPubKey));
            Assert.True(revoke.Subject.SequenceEqual(userPubKey));
            Assert.True(revoke.Verify(), "revoke was not verified");
        }
    }
}