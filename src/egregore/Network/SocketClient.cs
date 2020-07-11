// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace egregore.Network
{
    public sealed class SocketClient
    {
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);

        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);

        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        public static TextWriter @out;

        public static void StartClient(string hostNameOrAddress, int port)
        {
            try
            {
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList[0];
                var remoteEP = new IPEndPoint(ipAddress, port);

                var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                client.BeginConnect(remoteEP, x => ConnectCallback(x, @out), client);
                connectDone.WaitOne();

                Send(client, "This is a test<EOF>");
                sendDone.WaitOne();

                Receive(client, @out);
                receiveDone.WaitOne();
                
                @out.WriteLine("Response received : {0}", response);
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar, TextWriter @out)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                client.EndConnect(ar);
                @out.WriteLine("Socket connected to {0}", client.RemoteEndPoint);
                connectDone.Set();
            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client, TextWriter @out)
        {
            try
            {
                var state = new SocketState();
                state.workSocket = client;
                client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = (SocketState) ar.AsyncState;
                var client = state.workSocket;

                var bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, string data)
        {
            var byteData = Encoding.UTF8.GetBytes(data);
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                var bytesSent = client.EndSend(ar);
                @out.WriteLine("Sent {0} bytes to server.", bytesSent);
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}