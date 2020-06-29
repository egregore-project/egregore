// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data;
using Dapper;

namespace egregore
{
    internal sealed class UInt64TypeHandler : SqlMapper.TypeHandler<ulong?>
    {
        public override void SetValue(IDbDataParameter parameter, ulong? value) => parameter.Value = value;
        public override ulong? Parse(object value) => value is long v ? (ulong) v : !ulong.TryParse((string) value, out var val) ? default(ulong?) : val;
    }
}