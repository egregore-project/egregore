﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("egregore")]
[assembly: InternalsVisibleTo("egregore.Client")]
[assembly: InternalsVisibleTo("egregore.Tests")]
[assembly: InternalsVisibleTo("egregore.Benchmarks")]

namespace egregore
{
    // ReSharper disable once UnusedMember.Global
    [ExcludeFromCodeCoverage]
    internal static class InternalsVisibleTo
    {
    }
}