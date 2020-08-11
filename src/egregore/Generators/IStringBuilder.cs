// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace egregore.Generators
{
    public interface IStringBuilder
    {
        int Indent { get; set; }
        int Length { get; set; }

        IStringBuilder OpenNamespace(string @namespace);
        IStringBuilder CloseNamespace();
        IStringBuilder AppendLine(string message);
        IStringBuilder AppendLine();
        IStringBuilder Clear();
        IStringBuilder Insert(int index, object value);
        IStringBuilder Append(string value);
    }
}