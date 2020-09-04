// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using egregore.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Compliance
{
    public sealed class ThirdPartyFileGenerator
    {
        public ThirdPartyFileGenerator(ITestOutputHelper console)
        {
            _console = console;
        }

        private readonly ITestOutputHelper _console;

        private static readonly string[] DependencyPackageIds = {"egregore"};

        [Fact]
        public async Task Generate_third_party_file()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);
            await OpenSourceCompliance.CreateThirdPartyLicensesFile(
                DependencyPackageIds,
                new Dictionary<string, string>(), @out);
        }
    }
}