// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace ComplianceGen
{
    public static class NuGetClient
    {
        public static async Task<IPackageSearchMetadata> SearchForMetadataAsync(string packageId, NuGetVersion version,
            string packageSource = null, ILogger logger = null)
        {
            var identity = new PackageIdentity(packageId, version);
            var sourceRepository = packageSource == null ? GetSourceRepository() : GetSourceRepository(packageSource);
            var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            var searchMetadata = await packageMetadataResource.GetMetadataAsync(identity, new NullSourceCacheContext(),
                logger ?? new NullLogger(), CancellationToken.None);
            return searchMetadata;
        }

        public static async Task<IEnumerable<IPackageSearchMetadata>> SearchForPackageAsync(string packageId,
            string source = null, ILogger logger = null)
        {
            var sourceRepository = source == null ? GetSourceRepository() : GetSourceRepository(source);
            var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
            var searchFilter = new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion);
            var searchMetadata = await searchResource.SearchAsync(packageId, searchFilter, 0, 10,
                logger ?? new NullLogger(), CancellationToken.None);
            return searchMetadata;
        }

        public static async Task<IEnumerable<RemoteSourceDependencyInfo>> SearchForDependencyInfoAsync(string packageId,
            string source = null, ILogger logger = null)
        {
            var sourceRepository = source == null ? GetSourceRepository() : GetSourceRepository(source);
            var dependencyResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
            return await dependencyResource.ResolvePackages(packageId, new NullSourceCacheContext(),
                logger ?? new NullLogger(), CancellationToken.None);
        }

        public static NuGetFramework GetTargetFramework(Assembly assembly = null)
        {
            var frameworkName = (assembly ?? Assembly.GetCallingAssembly()).GetCustomAttributes(true)
                .OfType<TargetFrameworkAttribute>()
                .Select(x => x.FrameworkName)
                .FirstOrDefault();
            var currentFramework = frameworkName == null
                ? NuGetFramework.AnyFramework
                : NuGetFramework.ParseFrameworkName(frameworkName, new DefaultFrameworkNameProvider());
            return currentFramework;
        }

        private static SourceRepository GetSourceRepository(string source = "https://api.nuget.org/v3/index.json")
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var packageSource = new PackageSource(source);
            var sourceRepository = new SourceRepository(packageSource, providers);
            return sourceRepository;
        }
    }
}