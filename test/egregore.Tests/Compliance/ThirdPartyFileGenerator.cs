using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Compliance
{
    public sealed class ThirdPartyFileGenerator
    {
        private readonly ITestOutputHelper _console;

        private static readonly string[] DependencyPackageIds = { "Dapper", "libsodium", "LightningDB", "NetMQ", "WyHash"};
        private static readonly string[] DependencyPackageVersions = { "2.0.35", "1.0.18", "0.12.0", "4.0.0.207", "1.0.4"};

        public ThirdPartyFileGenerator(ITestOutputHelper console)
        {
            _console = console;
        }

        [Fact]
        public async Task Shallow_license_scan()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);
            await OpenSourceCompliance.ShallowLicenseScan(
                DependencyPackageIds, 
                DependencyPackageVersions,
                @out);
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
