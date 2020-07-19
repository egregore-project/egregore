using System;
using NuGet.Packaging.Core;

namespace egregore.Tests.Compliance
{
    public struct LicenseEntry
    {
        public PackageDependency PackageDependency { get; set; }
        public Uri LicenseUrl { get; set; }
    }
}