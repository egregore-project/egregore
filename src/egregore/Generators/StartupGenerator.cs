namespace egregore.Generators
{
    public class StartupGenerator 
    {
        public void Generate(IStringBuilder sb)
        {
            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Hosting;");
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Microsoft.Extensions.Hosting;");
            sb.AppendLine();

            sb.AppendLine(@$"public sealed class Startup");
            sb.AppendLine(@$"{{");
            sb.AppendLine(@$"    public IConfiguration Configuration {{ get; }}");
            sb.AppendLine(@$"    public Startup(IConfiguration configuration) => Configuration = configuration;");
            sb.AppendLine(@$"    public void ConfigureServices(IServiceCollection services) => services.AddGenerated(Configuration);");
            sb.AppendLine(@$"    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) => app.UseGenerated(env);");
            sb.AppendLine(@$"}}");
            sb.AppendLine();
        }
    }
}
