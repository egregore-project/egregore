using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using egregore.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Compliance
{
    public sealed class ThirdPartyFileGenerator
    {
        private readonly ITestOutputHelper _console;

        private static readonly string[] DependencyPackageIds = { "egregore"};
        
        public ThirdPartyFileGenerator(ITestOutputHelper console)
        {
            _console = console;
        }

        [Fact]
        public async Task Generate_third_party_file()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);
            await OpenSourceCompliance.CreateThirdPartyLicensesFile(
                DependencyPackageIds, 
                new Dictionary<string, string>(), @out);
        }
    }
}
