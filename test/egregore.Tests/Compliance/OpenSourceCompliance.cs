// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using egregore.Extensions;
using NuGet.Packaging.Core;

namespace egregore.Tests.Compliance
{
    internal static class OpenSourceCompliance
    {
        public static async Task<IEnumerable<LicenseEntry>> ShallowLicenseScan(string[] packageIds, string[] versions,
            TextWriter @out)
        {
            var result = new List<LicenseEntry>();

            for (var i = 0; i < packageIds.Length; i++)
            {
                var packageId = packageIds[i];
                var dependencySources = await NuGetClient.SearchForDependencyInfoAsync(packageId);

                if (versions[i] == null || versions[i] == "latest")
                    dependencySources = dependencySources.OrderByDescending(x => x.Identity.Version)
                        .Where(x => x.Listed).Take(1);
                else
                    dependencySources = dependencySources.Where(x => x.Identity.Version.ToString() == versions[i])
                        .Take(1);

                var dependencies = new HashSet<PackageDependency>();
                var licenseUrls = new HashSet<Uri>();

                foreach (var dependencySource in dependencySources)
                {
                    var line = $"{dependencySource.Identity.Id} {dependencySource.Identity.Version}";

                    @out.WriteLine();
                    @out.WriteLine(line);
                    WriteDashes(line.Length, @out.WriteLine);

                    foreach (var dependencyGroup in dependencySource.DependencyGroups)
                    foreach (var package in dependencyGroup.Packages)
                    {
                        @out.WriteLine($"{package.Id} ({package.VersionRange.MinVersion})");

                        if (!dependencies.Contains(package))
                        {
                            var metadata =
                                await NuGetClient.SearchForMetadataAsync(package.Id,
                                    package.VersionRange.MinVersion);
                            if (metadata == default)
                                continue;

                            licenseUrls.Add(metadata.LicenseUrl);
                            dependencies.Add(package);
                            result.Add(new LicenseEntry
                                {PackageDependency = package, LicenseUrl = metadata.LicenseUrl});
                        }
                    }
                }
            }


            return result;
        }

        public static async Task CreateThirdPartyLicensesFile(string[] packageIds,
            IReadOnlyDictionary<string, string> exceptions, TextWriter @out)
        {
            // See: https://github.com/dotnet/roslyn/issues/32022

            // .NET Library License:
            var dotNetLibraryReferences = new List<string>();
            dotNetLibraryReferences.Add("http://go.microsoft.com/fwlink/?LinkId=529443");
            dotNetLibraryReferences.Add("http://go.microsoft.com/fwlink/?LinkId=329770");
            dotNetLibraryReferences.Add("https://dotnet.microsoft.com/dotnet_library_license.htm");
            dotNetLibraryReferences.Add("https://go.microsoft.com/fwlink/?linkid=2028464");

            //
            // Azure SDK License
            var azureLibraryReferences = new List<string>();
            azureLibraryReferences.Add("https://aka.ms/netcoregaeula");

            var versions = new List<string>();
            for (var i = 0; i < packageIds.Length; i++)
                versions.Add("latest");

            var entries = await ShallowLicenseScan(packageIds, versions.ToArray(), @out);

            const string header = @"##################################################################

egregore-project uses third-party libraries and other materials, 
distributed under different licenses than egregore-project licensed
components.

If we have omitted a notice here, please let us know by submitting
an issue on GitHub.

##################################################################";

            const string fileName = "THIRD-PARTY-NOTICES.txt";
            if (File.Exists(fileName))
                File.Delete(fileName);

            var unlicensed = new HashSet<LicenseEntry>();

            await using var fs = File.OpenWrite(Path.Combine("..\\..\\..\\..\\..\\", fileName));
            await using var sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine(header);

            foreach (var entry in entries)
                try
                {
                    var url = entry.LicenseUrl;
                    if (entry.LicenseUrl == null)
                    {
                        @out.WriteErrorLine(
                            $"{entry.PackageDependency.Id} {entry.PackageDependency.VersionRange.MinVersion} does not have a license URL.");
                        unlicensed.Add(entry);
                        continue;
                    }

                    // If it's a GitHub link, rewrite as raw:
                    if (url.OriginalString.StartsWith("https://github.com/"))
                    {
                        var rawUrl = url.OriginalString
                            .Replace("https://github.com/", "https://raw.githubusercontent.com/")
                            .Replace("blob/master", "master");

                        url = new Uri(rawUrl, UriKind.Absolute);
                    }

                    // Swap any known exceptions:
                    if (exceptions.ContainsKey(url.OriginalString))
                        url = new Uri(exceptions[url.OriginalString], UriKind.Absolute);

                    string licenseText;
                    var client = new HttpClient();

                    if (dotNetLibraryReferences.Contains(url.OriginalString))
                    {
                        licenseText = GetDotNetLibraryLicense();
                    }
                    else if (azureLibraryReferences.Contains(url.OriginalString))
                    {
                        licenseText = "See ./licenses/Microsoft Azure DocumentDB SDK for .NET Core - RTM.docx";
                    }
                    else
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        var response = await client.SendAsync(request, CancellationToken.None);
                        if (!response.IsSuccessStatusCode)
                        {
                            @out.WriteErrorLine($"({response.StatusCode}) {url}");
                            continue;
                        }

                        licenseText = response.Content.ReadAsStringAsync().Result;
                    }

                    var now = DateTime.UtcNow;

                    // If it's a NuGet link, rewrite to the SPDX link:
                    var spdx =
                        $"https://www.nuget.org/packages/{entry.PackageDependency.Id}/{entry.PackageDependency.VersionRange.MinVersion}/license";
                    if (url.OriginalString == spdx)
                    {
                        var parser = new HtmlParser();
                        var document = parser.ParseDocument(licenseText);
                        var element = document.QuerySelectorAll("a").FirstOrDefault(x =>
                            x.Attributes["href"].Value.StartsWith("https://licenses.nuget.org/"));
                        if (element == null)
                        {
                            // NuGet has it listed as a custom license (likely needs further categorization):
                            element = document.QuerySelectorAll(".custom-license-container")
                                .FirstOrDefault();

                            licenseText = element.InnerHtml;
                        }
                        else
                        {
                            var href = (element.Attributes["href"].Value + ".txt")
                                .Replace("https://licenses.nuget.org/", "https://spdx.org/licenses/");

                            url = new Uri(href, UriKind.Absolute);

                            var request = new HttpRequestMessage(HttpMethod.Get, url);
                            var response = await client.SendAsync(request, CancellationToken.None);
                            if (!response.IsSuccessStatusCode)
                            {
                                @out.WriteErrorLine($"({response.StatusCode}) {url}");
                                continue;
                            }

                            licenseText = response.Content.ReadAsStringAsync().Result;
                        }
                    }

                    var line1 =
                        $"egregore-project uses {entry.PackageDependency.Id} {entry.PackageDependency.VersionRange.MinVersion}";
                    var line2 = $"License was downloaded from {url}";
                    var line3 = $"License was downloaded on {now.ToLongDateString()} {now.ToLongTimeString()}";

                    sw.WriteLine();
                    sw.WriteLine();
                    WriteDashes(Math.Max(line1.Length, Math.Max(line2.Length, line3.Length)), sw.WriteLine);
                    sw.WriteLine(line1);
                    sw.WriteLine(line2);
                    sw.WriteLine(line3);
                    WriteDashes(Math.Max(line1.Length, Math.Max(line2.Length, line3.Length)), sw.WriteLine);
                    sw.WriteLine();
                    sw.WriteLine(licenseText);
                }
                catch (Exception e)
                {
                    @out.WriteErrorLine(e.ToString());
                }

            if (unlicensed.Count > 0)
            {
                sw.WriteLine();
                sw.WriteLine("Unlicensed Dependencies:");
                sw.WriteLine("------------------------");
                foreach (var item in unlicensed)
                    sw.WriteLine(
                        $"{item.PackageDependency.Id} {item.PackageDependency.VersionRange.ToNormalizedString()}");
            }
        }

        private static string GetDotNetLibraryLicense()
        {
            var type = typeof(OpenSourceCompliance);
            var assembly = type.Assembly;
            using var stream = assembly.GetManifestResourceStream(type, "dotnet_library_license.txt");
            if (stream == default)
                throw new InvalidOperationException();
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        private static void WriteDashes(int length, Action<string> action)
        {
            var line = Enumerable.Repeat('-', length);
            var sb = new StringBuilder();
            foreach (var c in line)
                sb.Append(c);
            action(sb.ToString());
        }
    }
}