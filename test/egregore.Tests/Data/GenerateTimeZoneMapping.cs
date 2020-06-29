﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using egregore.Generators;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Data
{
    public class GenerateTimeZoneMapping
    {
        private readonly ITestOutputHelper _output;

        public GenerateTimeZoneMapping(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Can_generate_time_zone_LUT()
        {
            const string source = "https://github.com/unicode-cldr/cldr-core/blob/master/supplemental/windowsZones.json";
            
            var json = File.ReadAllText("TestData\\WindowsZones.json");
            var cldr = JsonConvert.DeserializeObject<CldrFile>(json);

            Assert.NotNull(cldr.Supplemental);
            Assert.NotNull(cldr.Supplemental.WindowsZones);
            Assert.NotEmpty(cldr.Supplemental.WindowsZones.MapTimeZones);

            var sb = new StringBuilder();
            sb.AppendAutoGeneratedHeader();
            
            sb.AppendLine("// ReSharper disable StringLiteralTypo");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Globalization;");
            sb.AppendLine("using System.Runtime.InteropServices;");
            sb.AppendLine();
            sb.AppendLine("namespace egregore.Data");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Source: {source}");
            sb.AppendLine($"    /// Unicode Version: {cldr.Supplemental.Version.UnicodeVersion}");
            sb.AppendLine($"    /// CLDR Version: {cldr.Supplemental.Version.CldrVersion}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine("    public static class TimeZoneLookup");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary> Determines the wall time and time zone for the current server. </summary>");
            sb.AppendLine("        public static IsoTimeZoneString Now");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");
            sb.AppendLine("                var now = DateTimeOffset.Now;");
            sb.AppendLine();
            sb.AppendLine("                // Non-Windows servers use IANA");
            sb.AppendLine("                if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))");
            sb.AppendLine("                    return new IsoTimeZoneString(now, TimeZoneInfo.Local.StandardName);");
            sb.AppendLine();
            sb.AppendLine("                // Windows servers use CLDR");
            sb.AppendLine("                var region = RegionInfo.CurrentRegion;");
            sb.AppendLine("                var regionName = region.EnglishName.Replace(' ', '_');");
            sb.AppendLine("                var localTimeZone = TimeZoneInfo.Local; ");
            sb.AppendLine();
            sb.AppendLine("                #region LUT");
            sb.AppendLine();
            sb.AppendLine("                switch(localTimeZone.StandardName)");
            sb.AppendLine("                {");
            sb.AppendLine("                    case \"Coordinated Universal Time\":");
            sb.AppendLine("                        return new IsoTimeZoneString(now, \"Etc\\UTC\");");

            // other, territory, type
            var map = new Dictionary<string, Dictionary<string, string>>();
            foreach (var mapTimeZone in cldr.Supplemental.WindowsZones.MapTimeZones)
            {
                Assert.NotNull(mapTimeZone.MapZone);
                if (!map.TryGetValue(mapTimeZone.MapZone.Other, out var lookup))
                    map.Add(mapTimeZone.MapZone.Other, lookup = new Dictionary<string, string>());
                lookup.Add(mapTimeZone.MapZone.Territory, mapTimeZone.MapZone.Type);
            }

            foreach (var (k, v) in map)
            {
                sb.AppendLine($"                    case \"{k}\":");
                sb.AppendLine($"                    {{");
                sb.AppendLine($"                        switch(region.TwoLetterISORegionName)");
                sb.AppendLine($"                        {{");
                foreach (var tuple in v
                .Reverse())
                {
                    var key = tuple.Key == "001" ? "default" : $"case \"{tuple.Key}\"";
                    sb.AppendLine($"                            {key}:");

                    var values = tuple.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length > 1)
                    {
                        var prefix = values[0].Split("/")[0];
                        sb.AppendLine($"                                switch($\"{prefix}/{{regionName}}\")");
                        sb.AppendLine($"                                {{");
                        foreach (var value in values)
                        {
                            sb.AppendLine($"                                    case \"{value}\":");
                            sb.AppendLine($"                                        return new IsoTimeZoneString(now, \"{value}\");");
                        }
                        sb.AppendLine($"                                    default:");
                        sb.AppendLine($"                                        return new IsoTimeZoneString(now, \"{values[0]}\");");
                        sb.AppendLine($"                                }}");
                    }
                    else
                    {
                        sb.AppendLine($"                                    return new IsoTimeZoneString(now, \"{tuple.Value}\");");
                    }
                }
                sb.AppendLine($"                        }}");
                sb.AppendLine($"                    }}");
            }

            sb.AppendLine("                    default:");
            sb.AppendLine($"                        throw new NotSupportedException($\"Missing time zone map from '{{localTimeZone.DisplayName}}'\");");
            sb.AppendLine("                }");
            sb.AppendLine();
            sb.AppendLine("                #endregion");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            _output.WriteLine(sb.ToString());
        }

        #region CLDR JSON Format

        // ReSharper disable once IdentifierTypo
        public sealed class CldrFile
        {
            public Supplemental Supplemental { get; set; }
        }

        public sealed class Supplemental
        {
            public Version Version { get; set; }
            public WindowsZones WindowsZones { get; set; }
        }

        public sealed class Version
        {
            [JsonProperty("_unicodeVersion")]
            public string UnicodeVersion { get; set; }

            [JsonProperty("_cldrVersion")]
            public string CldrVersion { get; set; }
        }

        public sealed class WindowsZones
        {
            public MapTimeZone[] MapTimeZones { get; set; }
        }

        public sealed class MapTimeZone
        {
            public MapZone MapZone { get; set; }
        }

        public sealed class MapZone
        {
            [JsonProperty("_other")]
            public string Other { get; set; }

            [JsonProperty("_type")]
            public string Type { get; set; }

            [JsonProperty("_territory")]
            public string Territory { get; set; }
        }

        #endregion
    }
}