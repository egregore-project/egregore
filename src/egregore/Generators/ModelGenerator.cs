// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Ontology;

namespace egregore.Generators
{
    public sealed class ModelGenerator
    {
        public void Generate(IStringBuilder sb, Namespace @namespace, ulong revision, Schema schema)
        {
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine();

            sb.OpenNamespace($"{@namespace.Value}.V{revision}");
            sb.AppendLine($"public sealed class {schema.Name}");
            sb.AppendLine("{");
            sb.AppendLine("}");
            sb.CloseNamespace();
        }
    }
}