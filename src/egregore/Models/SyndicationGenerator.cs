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
using egregore.Data;
using WyHash;

namespace egregore.Models
{
    internal sealed class SyndicationGenerator
    {
        public static bool TryBuildFeedAsync<T>(string url, string ns, ulong rs, IEnumerable<T> records,
            string mediaType, Encoding encoding, out byte[] stream, out DateTimeOffset? lastModified)
            where T : IRecord<T>
        {
            // FIXME: need to normalize this to produce stable IDs (i.e. when query strings are in a different order)
            var id = WyHash64.ComputeHash64(Encoding.UTF8.GetBytes(url),
                BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(SyndicationFeed))));

            var items = new List<SyndicationItem>();
            lastModified = default;
            foreach (var record in records)
            {
                var title = $"{typeof(T).Name}";
                var description = $"Location of the {typeof(T).Name} record at the specified feed item URI";
                var uri = $"/api/{ns}/v{rs}/{typeof(T).Name}/{record.Uuid}";

                var timestamp = TimestampFactory.FromUInt64(record.TimestampV2);
                var item = new SyndicationItem(title, description, new Uri(uri, UriKind.Relative), title, timestamp);
                items.Add(item);
                lastModified = timestamp;
            }

            var feed = new SyndicationFeed($"{typeof(T).Name} Query Feed",
                $"A feed containing {typeof(T).Name} records for the query specified by the feed URI'", new Uri(url),
                $"{id}", lastModified.GetValueOrDefault(DateTimeOffset.Now)) {Items = items};

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
                case Constants.MediaTypeNames.Application.RssXml:
                case Constants.MediaTypeNames.Text.Xml:
                {
                    formatter = new Rss20FeedFormatter(feed, false);
                    break;
                }
                case Constants.MediaTypeNames.Application.AtomXml:
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