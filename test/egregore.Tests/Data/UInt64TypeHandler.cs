// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Data;
using Dapper;

namespace egregore.Tests.Data
{
    internal sealed class UInt64TypeHandler : SqlMapper.TypeHandler<ulong?>
    {
        public override void SetValue(IDbDataParameter parameter, ulong? value)
        {
            parameter.Value = value;
        }

        public override ulong? Parse(object value)
        {
            return value is long v ? (ulong) v : !ulong.TryParse((string) value, out var val) ? default(ulong?) : val;
        }
    }
}