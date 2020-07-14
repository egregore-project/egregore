// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Noise;

namespace egregore.Network
{
    internal sealed class NoiseProtocol : IProtocol
    {
        private readonly bool _initiator;

        private static readonly Protocol Protocol = new Protocol(
            HandshakePattern.IK,
            CipherFunction.ChaChaPoly,
            HashFunction.Blake2b,
            PatternModifiers.Psk2
        );

        private readonly HandshakeState _state;
        private Transport _transport;

        public unsafe NoiseProtocol(bool initiator, byte* sk, byte* psk, byte[] publicKey = default)
        {
            _initiator = initiator;
            var s = Crypto.SigningKeyToEncryptionKey(sk);
            var psks = new List<PskRef> { PskRef.Create(psk) };
            _state = Protocol.Create(initiator, default, s, (int) Crypto.EncryptionKeyBytes, publicKey, psks);
        }

        public bool Handshake(IChannel sender, Socket handler)
        {
            if (_initiator)
            {
                ClientToServer(sender, handler);
            }
            else
            {
                ServerToClient(sender, handler);
            }
            return true;
        }

        private void ServerToClient(IChannel sender, Socket handler)
        {
            var buffer = new byte[Protocol.MaxMessageLength];

            // Receive the first handshake message from the client.
            var receiveBuffer = new byte[Protocol.MaxMessageLength];
            var bytesRead = handler.Receive(receiveBuffer);
            var message = receiveBuffer.AsSpan(0, bytesRead);
            _state.ReadMessage(message, buffer);

            // Send the second handshake message to the client.
            var (bytesWritten, _, transport) = _state.WriteMessage(null, buffer);
            sender.Send(handler, buffer.AsSpan(0, bytesWritten));
            _transport = transport;
        }

        private void ClientToServer(IChannel sender, Socket handler)
        {
            var buffer = new byte[Protocol.MaxMessageLength];

            // Send the first handshake message to the server.
            var (bytesWritten, _, _) = _state.WriteMessage(null, buffer);
            sender.Send(handler, buffer.AsSpan(0, bytesWritten));

            // Receive the second handshake message from the server.
            var receiveBuffer = new byte[Protocol.MaxMessageLength];
            var bytesRead = handler.Receive(receiveBuffer);

            var (_, _, transport) = _state.ReadMessage(receiveBuffer.AsSpan(0, bytesRead), buffer);
            _transport = transport;
        }

        public void OnMessageReceived(IChannel sender, Socket handler, ReadOnlySpan<byte> message)
        {
            var buffer = new byte[Protocol.MaxMessageLength];
            var bytesRead = _transport.ReadMessage(message, buffer);
            Console.WriteLine(Encoding.UTF8.GetString(buffer.AsSpan().Slice(0, bytesRead)));
        }

        public bool IsEndOfMessage(string message) => true;
        public bool HasTransport => _transport != null;
    }
}