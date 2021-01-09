﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;

namespace egregore.Web.Filters
{
    internal sealed class DailyCacheAttribute : ResponseCacheAttribute
    {
        public DailyCacheAttribute()
        {
            CacheProfileName = Constants.DailyCacheProfileName;
        }
    }

    internal sealed class YearlyCacheAttribute : ResponseCacheAttribute
    {
        public YearlyCacheAttribute()
        {
            CacheProfileName = Constants.YearlyCacheProfileName;
        }
    }
}