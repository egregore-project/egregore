// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using egregore.Pages;

namespace egregore.Models
{
    public class PageKeyBuilder
    {
        public byte[] GetKey(Page page)
        {
            return Encoding.UTF8.GetBytes($"P:{page.Uuid}");
        }

        public byte[] GetKey(Guid id)
        {
            return Encoding.UTF8.GetBytes($"P:{id}");
        }

        public byte[] GetAllKey()
        {
            return Encoding.UTF8.GetBytes("P:");
        }
    }
}