// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace egregore.Generators
{
    public sealed class ProjectGenerator
    {
        public void Generate(IStringBuilder sb)
        {
            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
            sb.AppendLine(1, "<PropertyGroup>");
            sb.AppendLine(2, "<TargetFramework>netcoreapp3.1</TargetFramework>");
            sb.AppendLine(2, "<IsPackable>false</IsPackable>");
            sb.AppendLine(2, "<IsPublishable>true</IsPublishable>");
            sb.AppendLine(1, "</PropertyGroup>");
            sb.AppendLine("</Project>");
        }
    }
}