// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Linq;
using egregore.Cryptography;

namespace egregore.Data
{
    internal static class LogEntryExtensions
    {
        public static void EntryCheck(this LogEntry entry, LogEntry previous, ILogEntryHashProvider hashProvider)
        {
            if (previous.Index + 1 != entry.Index)
            {
                var message = $"Invalid index: expected '{previous.Index + 1}' but was '{entry.Index}'";
                throw new LogException(message);
            }

            if (!previous.Hash.SequenceEqual(entry.PreviousHash))
            {
                var message =
                    $"Invalid previous hash: expected '{Crypto.ToHexString(previous.Hash)}' but was '{Crypto.ToHexString(entry.PreviousHash)}'";
                throw new LogException(message);
            }

            var hashRoot = hashProvider.ComputeHashRootBytes(entry);
            if (!hashRoot.SequenceEqual(entry.HashRoot))
            {
                var message =
                    $"Invalid hash root: expected '{Crypto.ToHexString(hashRoot)}' but was '{Crypto.ToHexString(entry.HashRoot)}'";
                throw new LogException(message);
            }

            for (var i = 0; i < entry.Objects.Count; i++)
            {
                if (entry.Objects[i].Index == i)
                    continue;
                var message = $"Invalid object index: expected '{i}' but was '{entry.Objects[i].Index}'";
                throw new LogException(message);
            }

            var hash = hashProvider.ComputeHashBytes(entry);
            if (!hash.SequenceEqual(entry.Hash))
            {
                var message =
                    $"Invalid hash: expected '{Crypto.ToHexString(hash)}' but was '{Crypto.ToHexString(entry.Hash)}'";
                throw new LogException(message);
            }
        }

        public static void RoundTripCheck(this LogEntry entry, ILogObjectTypeProvider typeProvider, byte[] secretKey)
        {
            var firstMemoryStream = new MemoryStream();
            var firstSerializeContext = new LogSerializeContext(new BinaryWriter(firstMemoryStream), typeProvider);

            byte[] nonce;
            if (secretKey != null)
            {
                nonce = SecretStream.Nonce();
                firstSerializeContext.bw.WriteVarBuffer(nonce);
                using var ems = new MemoryStream();
                using var ebw = new BinaryWriter(ems);
                var ec = new LogSerializeContext(ebw, typeProvider, firstSerializeContext.Version);
                entry.Serialize(ec, false);
                firstSerializeContext.bw.WriteVarBuffer(SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey));
            }
            else
            {
                firstSerializeContext.bw.Write(false);
                entry.Serialize(firstSerializeContext, false);
            }

            var originalData = firstMemoryStream.ToArray();

            var br = new BinaryReader(new MemoryStream(originalData));
            var deserializeContext = new LogDeserializeContext(br, typeProvider);
            nonce = deserializeContext.br.ReadVarBuffer();

            LogEntry deserialized;
            if (nonce != null)
            {
                using var dms =
                    new MemoryStream(SecretStream.DecryptMessage(deserializeContext.br.ReadVarBuffer(), nonce,
                        secretKey));
                using var dbr = new BinaryReader(dms);
                var dc = new LogDeserializeContext(dbr, typeProvider);
                deserialized = new LogEntry(dc);
            }
            else
            {
                deserialized = new LogEntry(deserializeContext);
            }

            var secondMemoryStream = new MemoryCompareStream(originalData);
            var secondSerializeContext = new LogSerializeContext(new BinaryWriter(secondMemoryStream), typeProvider);
            if (secretKey != null)
            {
                secondSerializeContext.bw.WriteVarBuffer(nonce);
                using var ems = new MemoryStream();
                using var ebw = new BinaryWriter(ems);
                var ec = new LogSerializeContext(ebw, typeProvider, secondSerializeContext.Version);
                deserialized.Serialize(ec, false);
                secondSerializeContext.bw.WriteVarBuffer(SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey));
            }
            else
            {
                secondSerializeContext.bw.Write(false);
                deserialized.Serialize(secondSerializeContext, false);
            }
        }
    }
}