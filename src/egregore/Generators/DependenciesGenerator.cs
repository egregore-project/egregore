// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Ontology;

namespace egregore.Generators
{
    public sealed class DependenciesGenerator
    {
        public void Generate(IStringBuilder sb, Namespace ns)
        {
            sb.AppendLine(@"using Microsoft.AspNetCore.Hosting;");
            sb.AppendLine(@"using Microsoft.Extensions.Hosting;");
            sb.AppendLine(@"using Microsoft.AspNetCore.Builder;");            
            sb.AppendLine(@"using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine(@"using Microsoft.Extensions.DependencyInjection.Extensions;");
            sb.AppendLine(@"using Microsoft.Extensions.Configuration;");
            sb.AppendLine(@"using Microsoft.Extensions.Options;");
            sb.AppendLine();

            sb.OpenNamespace(ns.Value);
            
            AppendAdd(sb);
            AppendUse(sb);

            sb.CloseNamespace();
        }

        private static void AppendAdd(IStringBuilder sb)
        {
            sb.AppendLine(@$"public static class Add");
            sb.AppendLine(@$"{{");
            sb.AppendLine(@$"    public static IServiceCollection AddGenerated(this IServiceCollection services, IConfiguration config)");
            sb.AppendLine(@$"    {{");
            sb.AppendLine(@$"        services.AddControllersWithViews();");
            sb.AppendLine(@$"        return services;");
            sb.AppendLine(@$"    }}");
            sb.AppendLine(@$"}}");
            sb.AppendLine();
        }

        private static void AppendUse(IStringBuilder sb)
        {
            sb.AppendLine(@$"public static class Use");
            sb.AppendLine(@$"{{");
            sb.AppendLine(@$"    public static IApplicationBuilder UseGenerated(this IApplicationBuilder app, IWebHostEnvironment env)");
            sb.AppendLine(@$"    {{");
            sb.AppendLine(@$"        if (env.IsDevelopment())");
            sb.AppendLine(@$"        {{");
            sb.AppendLine(@$"              app.UseDeveloperExceptionPage();");
            sb.AppendLine(@$"        }}");
            sb.AppendLine();
            sb.AppendLine(@$"        app.UseHttpsRedirection();");
            sb.AppendLine(@$"        app.UseStaticFiles();");
            sb.AppendLine(@$"        app.UseRouting();");
            sb.AppendLine(@$"        app.UseAuthentication();");
            sb.AppendLine(@$"        app.UseAuthorization();");
            sb.AppendLine(@$"        app.UseEndpoints(endpoints =>");
            sb.AppendLine(@$"        {{");
            sb.AppendLine(@$"            endpoints.MapControllers();");
            sb.AppendLine(@$"            endpoints.MapDefaultControllerRoute();");
            sb.AppendLine(@$"        }});");
            sb.AppendLine();
            sb.AppendLine(@$"        return app;");
            sb.AppendLine(@$"    }}");
            sb.AppendLine(@$"}}");
            sb.AppendLine();
        }
    }
}