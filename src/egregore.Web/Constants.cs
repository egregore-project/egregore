// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace egregore.Web
{
    internal static class Constants
    {
        public const string DailyCacheProfileName = "Daily";
        public const int OneDayInSeconds = 86_400;

        public const string YearlyCacheProfileName = "Yearly";
        public const int OneYearInSeconds = 31_557_600;

        public static class HeaderNames
        {
            public const string Accept = "Accept";
            public const string ContentDisposition = "Content-Disposition";
            public const string ContentSecurityPolicy = "Content-Security-Policy";
            public const string ContentType = "Content-Type";
            public const string PermissionsPolicy = "Permissions-Policy";
            public const string PublicKeyPins = "Public-Key-Pins";
            public const string ReferrerPolicy = "Referrer-Policy";

            public const string XContentTypeOptions = "X-Content-Type-Options";
            public const string XFrameOptions = "X-Frame-Options";
            public const string XTotalCount = "X-Total-Count";
        }

        public static class MediaTypeNames
        {
            public static class Application
            {
                public const string RssXml = "application/rss+xml";
                public const string AtomXml = "application/atom+xml";
                public const string ProblemJson = "application/problem+json";
            }

            public static class Image
            {
                public const string Png = "image/png";
            }

            public static class Text
            {
                public const string Markdown = "text/markdown";
                public const string Xml = "text/xml";
                public const string Html = "text/html";
                public const string Plain = "text/plain";
            }
        }
    }
}