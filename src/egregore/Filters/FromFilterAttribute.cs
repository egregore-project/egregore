// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace egregore.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class FromFilterAttribute : BindingBehaviorAttribute
    {
        public FromFilterAttribute() : base(BindingBehavior.Never) { }
    }


}