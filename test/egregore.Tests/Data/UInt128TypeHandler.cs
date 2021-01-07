// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Data;
using Dapper;
using egregore.Data;

namespace egregore.Tests.Data
{
    internal sealed class UInt128TypeHandler : SqlMapper.TypeHandler<UInt128?>
    {
        public override void SetValue(IDbDataParameter parameter, UInt128? value)
        {
            parameter.Value = value.GetValueOrDefault().ToString();
        }

        public override UInt128? Parse(object value)
        {
            return value is UInt128 v ? v : new UInt128((string) value);
        }
    }
}