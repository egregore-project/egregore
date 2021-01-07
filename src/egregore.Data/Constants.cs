// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace egregore.Data
{
    internal static class Constants
    {
        public const string DefaultNamespace = "Default";
        public const string DefaultSequence = "global";
        public const string DefaultOwnerRole = "owner";
        public const int DefaultPort = 5001;

        public static readonly string DefaultRootPath = ".egregore";

        public static class EnvVars
        {
            public const string KeyFilePassword = "EGREGORE_KEY_FILE_PASSWORD";
            public const string EggFilePath = "EGREGORE_EGG_FILE_PATH";
        }

        public class Commands
        {
            public const string GrantRole = "grant_role";
            public const string RevokeRole = "revoke_role";
        }

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
            }
        }

        public class Notifications
        {
            public const string ReceiveMessage = nameof(ReceiveMessage);
        }

        public static class StorageTypes
        {
            public const string String = "string";
        }
    }
}