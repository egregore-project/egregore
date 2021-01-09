// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComplianceGen
{
    internal static class Program
    {
        private static readonly string[] DefaultDependencyPackageIds = {"egregore"};

        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
                args = DefaultDependencyPackageIds;

            await OpenSourceCompliance.CreateThirdPartyLicensesFile(args, new Dictionary<string, string>(),
                Console.Out);
        }
    }
}