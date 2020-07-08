// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data;
using Dapper;

namespace egregore.Data
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