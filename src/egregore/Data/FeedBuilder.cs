// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using WyHash;

namespace egregore.Data
{
    internal sealed class FeedBuilder
    {
        public static bool TryBuildFeedAsync<T>(string url, string ns, ulong rs, IEnumerable<T> records,
            string mediaType, Encoding encoding, out byte[] stream) where T : IRecord<T>
        {
            var timestamp = TimeZoneLookup.Now.Timestamp;
                
            // FIXME: need to normalize this to produce stable IDs (i.e. when query strings are in a different order)
            var id = WyHash64.ComputeHash64(Encoding.UTF8.GetBytes(url), BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(SyndicationFeed)))); 

            var feed = new SyndicationFeed($"{typeof(T).Name} Query Feed",
                $"A feed containing {typeof(T).Name} records for the query specified by the feed URI'", new Uri(url),
                $"{id}", timestamp);

            var items = new List<SyndicationItem>();
            foreach (var record in records)
            {
                var title = $"{typeof(T).Name}";
                var description = $"Location of the {typeof(T).Name} record at the specified feed item URI";
                var uri = $"/api/{ns}/v{rs}/{typeof(T).Name}/{record.Uuid}";
                var ts = timestamp; // FIXME: need to surface record creation timestamps

                var item = new SyndicationItem(title, description, new Uri(uri, UriKind.Relative), title, ts);
                items.Add(item);
            }

            feed.Items = items;

            var settings = new XmlWriterSettings
            {
                Encoding = encoding,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = false,
                Indent = true
            };

            SyndicationFeedFormatter formatter;
            switch (mediaType)
            {
                case Constants.MediaTypeNames.ApplicationRssXml:
                case Constants.MediaTypeNames.TextXml:
                {
                    formatter = new Rss20FeedFormatter(feed, false);
                    break;
                }
                case Constants.MediaTypeNames.ApplicationAtomXml:
                {
                    formatter = new Atom10FeedFormatter(feed);
                    break;
                }
                default:
                {
                    stream = default;
                    return false;
                }
            }

            using var ms = new MemoryStream();
            using var writer = XmlWriter.Create(ms, settings);
            formatter.WriteTo(writer);
            writer.Flush();

            stream = ms.ToArray();
            return true;
        }
    }
}