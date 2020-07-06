﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Extensions;

namespace egregore.Ontology
{
    public abstract class Privilege : ILogSerialized
    {
        public byte[] Subject { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public byte[] Authority { get; set; }
        public byte[] Signature { get; private set; }

        protected Privilege(string type)
        {
            Type = type;
        }

        protected Privilege(string type, byte[] signature)
        {
            Type = type;
            Signature = signature;
        }

        public void Sign(IKeyFileService keyFileService, IKeyCapture capture, ulong formatVersion = LogSerializeContext.FormatVersion)
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