// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.CodeGeneration;
using egregore.Data;

namespace egregore.Models
{
    public sealed class ModelGenerator
    {
        public void Generate(IStringBuilder sb, Namespace @namespace, ulong revision, Schema schema)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using egregore.Data;");
            sb.AppendLine("using egregore.Data.Attributes;");
            sb.AppendLine();

            sb.OpenNamespace($"{@namespace.Value}.V{revision}");

            sb.AppendLine($"public sealed class {schema.Name} : {nameof(IRecord<object>)}<{schema.Name}>");
            sb.AppendLine("{");

            sb.Indent++;
            {
                sb.AppendLine("[ReadOnly(true)]");
                sb.AppendLine("public Guid Uuid { get; set; }");

                sb.AppendLine("[ReadOnly(true)]");
                sb.AppendLine("public ulong TimestampV1 { get; set; }");

                sb.AppendLine("[ReadOnly(true)]");
                sb.AppendLine("public ulong TimestampV2 { get; set; }");

                foreach (var property in schema.Properties)
                {
                    if (property.IsRequired)
                        sb.AppendLine("[Required]");

                    sb.AppendLine($"public {property.Type} {property.Name} {{ get; set; }}");
                }

                sb.AppendLine();
                sb.AppendLine("public Record ToRecord()");
                sb.AppendLine("{");
                sb.AppendLine($"    var record = new Record {{ Type = \"{schema.Name}\" }};");
                sb.AppendLine("    record.Uuid = Uuid;");
                sb.AppendLine("    record.TimestampV1 = TimestampV1;");
                sb.AppendLine("    record.TimestampV2 = TimestampV2;");
                for (var i = 0; i < schema.Properties.Count; i++)
                {
                    var property = schema.Properties[i];
                    sb.AppendLine(
                        $"    record.Columns.Add(new RecordColumn({i}, \"{property.Name}\", \"{property.Type}\", {property.Name}?.ToString()));");
                }

                sb.AppendLine("    return record;");
                sb.AppendLine("}");

                sb.AppendLine();
                sb.AppendLine($"public {schema.Name} ToModel(Record record)");
                sb.AppendLine("{");
                sb.AppendLine($"    var model = new {schema.Name}();");
                sb.AppendLine("    model.Uuid = record.Uuid;");
                sb.AppendLine("    model.TimestampV1 = record.TimestampV1;");
                sb.AppendLine("    model.TimestampV2 = record.TimestampV2;");
                foreach (var property in schema.Properties)
                {
                    sb.AppendLine(
                        $"    var value = record.Columns.SingleOrDefault(x => x.Name.Equals(\"{property.Name}\", StringComparison.OrdinalIgnoreCase))?.Value;");
                    sb.AppendLine(
                        $"    model.{property.Name} = ({property.Type})(value == default ? default : Convert.ChangeType(value, typeof({property.Type})));");
                }

                sb.AppendLine("    return model;");
                sb.AppendLine("}");
            }
            sb.Indent--;


            sb.AppendLine("}");
            sb.CloseNamespace();
        }
    }
}