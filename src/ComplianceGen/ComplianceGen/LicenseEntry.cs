// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using NuGet.Packaging.Core;

namespace ComplianceGen
{
    public struct LicenseEntry
    {
        public PackageDependency PackageDependency { get; set; }
        public Uri LicenseUrl { get; set; }
    }
}