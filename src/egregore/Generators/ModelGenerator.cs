// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Ontology;

namespace egregore.Generators
{
    public sealed class ModelGenerator
    {
        public void Generate(IStringBuilder sb, Namespace @namespace, ulong revision, Schema schema)
        {
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using System.Data;");            
            sb.AppendLine($"using System.Text;");
            sb.AppendLine();

            sb.OpenNamespace($"{@namespace.Value}.V{revision}");
            sb.AppendLine($"public sealed class {schema.Name}");
            sb.AppendLine($"{{");
            sb.AppendLine($"}}");
            sb.CloseNamespace();
        }
    }
}