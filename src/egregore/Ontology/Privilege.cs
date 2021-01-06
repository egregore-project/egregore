// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Cryptography;
using egregore.Extensions;

namespace egregore.Ontology
{
    public abstract class Privilege : ILogSerialized
    {
        protected Privilege(string type, byte[] signature = null)
        {
            Type = type;
            Signature = signature;
        }

        public byte[] Subject { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public byte[] Authority { get; set; }
        public byte[] Signature { get; private set; }

        public void Sign(IKeyFileService keyFileService, IKeyCapture capture,
            ulong formatVersion = LogSerializeContext.FormatVersion)
        {
            unsafe
            {
                var sk = keyFileService.GetSecretKeyPointer(capture);
                var message = GetMessage(formatVersion);

                var signature = new byte[Crypto.SecretKeyBytes].AsSpan();
                var signatureLength = Crypto.SignDetached(message, sk, signature);
                if (signatureLength < (ulong) signature.Length)
                    signature = signature.Slice(0, (int) signatureLength);

                Signature = signature.ToArray();
            }
        }

        private string GetMessage(ulong formatVersion = LogSerializeContext.FormatVersion)
        {
            return $"v{formatVersion}_ed25519_" +
                   $"{Crypto.ToHexString(Authority)}_" +
                   $"{Type}_{Value}_to_" +
                   $"{Crypto.ToHexString(Subject)}";
        }

        public bool Verify(ulong formatVersion = LogSerializeContext.FormatVersion)
        {
            return Crypto.VerifyDetached(GetMessage(formatVersion), Signature, Authority);
        }

        #region Serialization

        protected Privilege(LogDeserializeContext context)
        {
            Subject = context.br.ReadVarBuffer();
            Type = context.br.ReadString();
            Value = context.br.ReadString();
            Authority = context.br.ReadVarBuffer();
            Signature = context.br.ReadVarBuffer();
        }

        public void Serialize(LogSerializeContext context, bool hash)
        {
            context.bw.WriteVarBuffer(Subject);
            context.bw.Write(Type);
            context.bw.Write(Value);
            context.bw.WriteVarBuffer(Authority);
            context.bw.WriteVarBuffer(Signature);
        }

        #endregion
    }
}