// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Generators
{
    public class ProgramGenerator
    {
        public void Generate(IStringBuilder sb)
        {
            sb.AppendLine(@"using Microsoft.AspNetCore.Hosting;");
            sb.AppendLine(@"using Microsoft.Extensions.Hosting;");
            sb.AppendLine();

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
        }
    }
}