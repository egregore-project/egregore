// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Ontology;

namespace egregore.Generators
{
    public class ProgramGenerator
    {
        public void Generate(IStringBuilder sb, Namespace ns)
        {
            sb.AppendLine(@"using Microsoft.AspNetCore.Hosting;");
            sb.AppendLine(@"using Microsoft.Extensions.Hosting;");
            sb.AppendLine();

            sb.OpenNamespace(ns.Value);

            sb.AppendLine(@"public sealed class Program");
            sb.AppendLine(@"{");
            sb.AppendLine();
            sb.AppendLine(@"    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();");
            sb.AppendLine(@"    public static IHostBuilder CreateHostBuilder(string[] args) => 
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => 
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                });");
            sb.AppendLine(@"}");

            sb.CloseNamespace();
        }
    }
}