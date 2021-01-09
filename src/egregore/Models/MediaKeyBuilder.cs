// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using egregore.Media;

namespace egregore.Models
{
    public class MediaKeyBuilder
    {
        public byte[] GetKey(MediaEntry media)
        {
            return Encoding.UTF8.GetBytes($"M:{media.Uuid}");
        }

        public byte[] GetKey(Guid id)
        {
            return Encoding.UTF8.GetBytes($"M:{id}");
        }

        public byte[] GetAllKey()
        {
            return Encoding.UTF8.GetBytes("M:");
        }
    }
}