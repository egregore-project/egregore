// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using egregore.Extensions;

namespace egregore.Network
{
    public sealed class SocketClient
    {
        private readonly ManualResetEvent _connected = new ManualResetEvent(false);
        private readonly ManualResetEvent _sent = new ManualResetEvent(false);
        private readonly ManualResetEvent _received = new ManualResetEvent(false);
        private readonly Encoding _encoding = Encoding.UTF8;

        private string _response = string.Empty;
        private readonly TextWriter _out;
        private Socket _socket;
        private readonly string _id;

        public SocketClient(string id = default, TextWriter @out = default)
        {
            _id = id ?? "[CLIENT]";
            _out = @out;
        }

        public void Connect(string hostNameOrAddress, int port)
        {
            var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
            var ipAddress = ipHostInfo.AddressList[0];
            var endpoint = new IPEndPoint(ipAddress, port);

            try
            {
                var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                client.BeginConnect(endpoint, ConnectCallback, client);
                _connected.WaitOne();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }

        public void SendTestMessage()
        {
            if (_socket == default)
                throw new InvalidOperationException($"{_id}: Must be connected before sending a message");

            try
            {
                SendMessage(_socket, "This is a test<EOF>");
                _sent.WaitOne();

                ReceiveMessage(_socket);
                _received.WaitOne();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
            finally
            {
                _sent.Reset();
                _received.Reset();
            }
        }

        public void Disconnect()
        {
            if (_socket == default)
                throw new InvalidOperationException($"{_id}: Must be connected before disconnecting");

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
            finally
            {
                _socket.Dispose();
                _socket = default;
                _connected.Reset();
            }
        }

        public void ConnectAndSendTestMessage(string hostNameOrAddress, int port)
        {
            var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
            var ipAddress = ipHostInfo.AddressList[0];
            var endpoint = new IPEndPoint(ipAddress, port);

            var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.BeginConnect(endpoint, ConnectCallback, client);
                _connected.WaitOne();

                SendMessage(client, "This is a test<EOF>");
                _sent.WaitOne();

                ReceiveMessage(client);
                _received.WaitOne();

                _out?.WriteInfoLine($"{_id}: Response received : {_response}");
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
                _out?.WriteErrorLine($"{_id}: {e}");
            }
            finally
            {
                client.Dispose();
                _connected.Reset();
                _sent.Reset();
                _received.Reset();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var socket = (Socket) ar.AsyncState;
                socket.EndConnect(ar);
                _out?.WriteInfoLine($"{_id}: Socket connected to {socket.RemoteEndPoint}");
                _connected.Set();
                _socket = socket;
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }

        private void ReceiveMessage(Socket client)
        {
            try
            {
                var state = new SocketState {Handler = client};
                if (client.Connected)
                {
                    client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
                }
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = (SocketState) ar.AsyncState;
                var client = state.Handler;

                var bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(_encoding.GetString(state.buffer, 0, bytesRead));
                    client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
                }
                else
                {
                    if (state.sb.Length > 1)
                        _response = state.sb.ToString();
                    _received.Set();
                }
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }

        private void SendMessage(Socket handler, string message)
        {
            var byteData = _encoding.GetBytes(message);
            handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                var sent = client.EndSend(ar);
                _out?.WriteInfoLine($"{_id}: Sent {sent} bytes to server.");
                _sent.Set();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }
    }
}