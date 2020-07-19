// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using egregore.IO;
using egregore.Ontology;
using egregore.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    [Collection("Serial")]
    public class RoleAccessTests
    {
        public RoleAccessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        [Fact]
        public void Can_grant_and_revoke_roles_between_users()
        {
            var capture = new PlaintextKeyCapture("rosebud", "rosebud");
            var service = new TempKeyFileService();

            var rootPubKey = CryptoTestHarness.GenerateKeyFile(_output, capture, service);
            capture.Reset();

            var userPubKey = CryptoTestHarness.GenerateKeyFile(_output, capture, new TempKeyFileService());
            capture.Reset();

            var grant = new GrantRole("admin", rootPubKey, userPubKey);
            grant.Sign(service, capture);

            Assert.True(grant.Authority.SequenceEqual(rootPubKey));
            Assert.True(grant.Subject.SequenceEqual(userPubKey));
            Assert.True(grant.Verify(), "grant was not verified");

            capture.Reset();
            var revoke = new RevokeRole(Constants.DefaultOwnerRole, rootPubKey, userPubKey);
            revoke.Sign(service, capture);

            Assert.True(revoke.Authority.SequenceEqual(rootPubKey));
            Assert.True(revoke.Subject.SequenceEqual(userPubKey));
            Assert.True(revoke.Verify(), "revoke was not verified");
        }
    }
}