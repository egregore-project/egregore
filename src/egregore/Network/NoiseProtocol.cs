// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using egregore.Extensions;
using NetMQ;
using Noise;

namespace egregore.Network
{
    internal sealed class NoiseProtocol : IProtocol, IDisposable
    {
        private readonly bool _initiator;
        private readonly string _id;
        private readonly TextWriter _out;

        private static readonly Protocol Protocol = new Protocol(
            HandshakePattern.IK,
            CipherFunction.ChaChaPoly,
            HashFunction.Blake2b,
            PatternModifiers.Psk2
        );

        private readonly HandshakeState _state;
        private readonly TimeSpan _interval;
        private readonly ThreadLocal<byte[]> _buffer;
        private Transport _transport;

        public unsafe NoiseProtocol(bool initiator, byte* sk, PskRef psk, byte[] publicKey = default, string id = default, TextWriter @out = default)
        {
            _initiator = initiator;
            _id = id ?? "[NOISE]";
            _out = @out;
            var psks = new List<PskRef> { psk };
            _state = Protocol.Create(initiator, default, sk, (int) Crypto.EncryptionKeyBytes, publicKey, psks);
            _interval = TimeSpan.FromSeconds(1);

            _buffer = new ThreadLocal<byte[]>(() => new byte[Protocol.MaxMessageLength]);
        }

        public void Configure(SocketOptions options)
        {
            options.MaxMsgSize = Protocol.MaxMessageLength;
            options.ReceiveBuffer = Protocol.MaxMessageLength;
            options.SendBuffer = Protocol.MaxMessageLength;
        }

        public bool Handshake(NetMQSocket handler) => _initiator ? TryClientHandshake(handler) : TryServerHandshake(handler);
        
        private bool TryServerHandshake(NetMQSocket handler)
        {
            // Receive the first handshake message from the client.
            _out?.WriteInfoLine($"{_id}: Waiting for first handshake");
            if (!handler.TryReceiveFrameBytes(_interval, out var message))
                return false;

            try
            {
                _out?.WriteInfoLine($"{_id}: Received first handshake");
                _state.ReadMessage(message, _buffer.Value);
            }
            catch (CryptographicException e)
            {
                _out?.WriteErrorLine($"{_id}: First handshake failed: {e.Message}");
                return false;
            }

            // Send the second handshake message to the client.
            _out?.WriteInfoLine($"{_id}: Sending second handshake");
            var (bytesWritten, _, transport) = _state.WriteMessage(null, _buffer.Value);
            var data = _buffer.Value.AsSpan(0, bytesWritten).ToArray();

            if (!handler.TrySendFrame(_interval, data))
            {
                _out?.WriteErrorLine($"{_id}: Failed to send second handshake");
                return false;
            }

            _transport = transport;
            return true;
        }

        private bool TryClientHandshake(NetMQSocket handler)
        {
            // Send the first handshake message to the server.
            _out?.WriteInfoLine($"{_id}: Sending first handshake");
            var (bytesWritten, _, _) = _state.WriteMessage(null, _buffer.Value);
            
            var data = _buffer.Value.AsSpan(0, bytesWritten).ToArray();
            if (!handler.TrySendFrame(_interval, data))
                return false;

            // Receive the second handshake message from the server.
            _out?.WriteInfoLine($"{_id}: Waiting for second handshake");
            if (!handler.TryReceiveFrameBytes(_interval, out var message))
                return false;

            _out?.WriteInfoLine($"{_id}: Received second handshake");
            var (_, _, transport) = _state.ReadMessage(message, _buffer.Value);
            _transport = transport;
            return true;
        }

        public void OnMessageReceived(NetMQSocket handler, ReadOnlySpan<byte> payload)
        {
            var message = Decrypt(payload);
            _out?.WriteInfoLine($"{_id}: Received encrypted payload");
            if(!_initiator)
                OnMessageSending(handler, Encoding.UTF8.GetBytes("OK"));
        }

        public void OnMessageSending(NetMQSocket handler, ReadOnlySpan<byte> payload)
        {
            var message = Encrypt(payload);
            if(handler.TrySendFrame(_interval, message))
                _out?.WriteInfoLine($"{_id}: Sent encrypted payload");
        }

        private byte[] Encrypt(ReadOnlySpan<byte> payload)
        {
            var bytesWritten = _transport.WriteMessage(payload, _buffer.Value);
            var data = _buffer.Value.AsSpan().Slice(0, bytesWritten);
            var message = data.ToArray();
            return message;
        }

        private byte[] Decrypt(ReadOnlySpan<byte> payload)
        {
            var bytesRead = _transport.ReadMessage(payload, _buffer.Value);
            var data = _buffer.Value.AsSpan().Slice(0, bytesRead);
            return data.ToArray();
        }

        public void Dispose()
        {
            _state?.Dispose();
            _transport?.Dispose();
        }
    }
}