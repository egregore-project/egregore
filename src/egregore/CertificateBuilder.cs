// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using egregore.Data;
using egregore.Extensions;

namespace egregore
{
    internal static class CertificateBuilder
    {
        private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
        private const string IssuerName = "egregore";
        private const string Organization = "https://egregore-project.org";

        public static X509Certificate2 GetOrCreateSelfSignedCert(TextWriter @out, bool fresh = false)
        {
            var now = TimeZoneLookup.Now;
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
                foreach (var result in store.Certificates.Find(X509FindType.FindByIssuerName, IssuerName, true))
                {
                    if (fresh || IsExpired(result, now))
                    {
                        @out?.WriteInfoLine($"Removing root certificate '{result.Thumbprint}'");
                        store.Remove(result);
                    }
                }
            }
            finally
            {
                store.Close();    
            }
            
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var result in store.Certificates.Find(X509FindType.FindByIssuerName, IssuerName, true))
                {
                    @out?.WriteInfoLine($"Using root certificate '{result.Thumbprint}'");
                    return result;
                }
            }
            finally
            {
                store.Close();    
            }

            @out?.WriteInfo("Generating new root certificate... ");

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            var countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;

            var sb = new StringBuilder();
            sb.Append($"CN=\"{IssuerName}\"");
            sb.Append($",OU=\"Copyright © {now.Timestamp:yyyy} {Organization}\"");
            sb.Append($",O=\"{Organization.Replace("\"", "\"\"")}\"");
            sb.Append($",C={countryCode.ToUpper()}");

            var distinguishedName = new X500DistinguishedName(sb.ToString());

            var alg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (AsymmetricAlgorithm) RSA.Create(2048)
                : ECDsa.Create(ECDiffieHellman.Create().ExportParameters(true));

            var request = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new CertificateRequest(distinguishedName, (RSA) alg, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1)
                : new CertificateRequest(distinguishedName, (ECDsa) alg, HashAlgorithmName.SHA512);

            // omit usage for encryption as RSA has vulnerabilities there
            var usages = X509KeyUsageFlags.DigitalSignature;
            usages |= X509KeyUsageFlags.KeyCertSign; // identify as a CA

            // extend as certificate authority
            request.CertificateExtensions.Add(new X509KeyUsageExtension(usages, false));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 1, true)); // identify as a CA
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection {new Oid(ServerAuthenticationOid)}, false));
            request.CertificateExtensions.Add(sanBuilder.Build());
                    
            var after = now.Timestamp.AddDays(-1).AddDays(2); // possibly annoying, but better than keeping roots around
            var before = now.Timestamp;
            var certificate = request.CreateSelfSigned(before, after);
            certificate.FriendlyName = IssuerName;

            try
            {
                var data = certificate.Export(X509ContentType.Pfx);
                var cert = new X509Certificate2(data);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);

                @out?.WriteInfoLine($"done");
                @out?.WriteInfoLine($"Using root certificate '{cert.Thumbprint}'");
                return cert;
            }
            finally
            {
               
                store.Close();  
            }
        }

        private static bool IsExpired(X509Certificate result, IsoTimeZoneString now)
        {
            var expirationString = result.GetExpirationDateString();
            return DateTimeOffset.TryParse(expirationString, out var expires) && expires <= now.Timestamp;
        }

        public static void ClearAll(TextWriter @out)
        {
            @out?.WriteWarningLine($"Removing all {IssuerName} issued root certificates.");

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
                foreach (var result in store.Certificates.Find(X509FindType.FindByIssuerName, IssuerName, true))
                {
                    @out?.WriteInfoLine($"Removing root certificate '{result.Thumbprint}'");
                    store.Remove(result);
                }
            }
            finally
            {
                store.Close();    
            }
        }
    }
}