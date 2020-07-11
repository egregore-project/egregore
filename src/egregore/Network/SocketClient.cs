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
        private readonly ManualResetEvent _connectDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _sendDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _receiveDone = new ManualResetEvent(false);
        private readonly Encoding _encoding = Encoding.UTF8;

        private string _response = string.Empty;
        private readonly TextWriter _out;

        public SocketClient(TextWriter @out = default)
        {
            _out = @out;
        }

        public void Start(string hostNameOrAddress, int port)
        {
            try
            {
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList[0];
                var endpoint = new IPEndPoint(ipAddress, port);

                var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                client.BeginConnect(endpoint, ConnectCallback, client);
                _connectDone.WaitOne();

                Send(client, "This is a test<EOF>");
                _sendDone.WaitOne();

                Receive(client);
                _receiveDone.WaitOne();
                
                _out?.WriteLine("Response received : {0}", _response);
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                client.EndConnect(ar);
                _out?.WriteLine("Socket connected to {0}", client.RemoteEndPoint);
                _connectDone.Set();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                var state = new SocketState {workSocket = client};
                client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = (SocketState) ar.AsyncState;
                var client = state.workSocket;

                var bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(_encoding.GetString(state.buffer, 0, bytesRead));
                    client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        _response = state.sb.ToString();
                    }
                    _receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
            }
        }

        private void Send(Socket client, string data)
        {
            var byteData = _encoding.GetBytes(data);
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                var bytesSent = client.EndSend(ar);
                _out?.WriteInfoLine("Sent {0} bytes to server.", bytesSent);
                _sendDone.Set();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine(e.ToString());
            }
        }
    }
}