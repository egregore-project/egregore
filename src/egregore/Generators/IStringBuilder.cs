// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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