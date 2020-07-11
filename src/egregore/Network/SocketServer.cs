using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace egregore.Network
{
    // Current code is based on: https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example

    public sealed class SocketServer
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        
        public static TextWriter @out;
        public static TextReader @in;

        public static void StartListening(string hostNameOrIpAddress, int port)
        {
            var ipHostInfo = Dns.GetHostEntry(hostNameOrIpAddress);
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, port);

            var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();
                    @out.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(AcceptCallback, listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }

            @out.WriteLine("\nPress ENTER to continue...");
            @in.Read();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            var listener = (Socket) ar.AsyncState;
            var handler = listener.EndAccept(ar);
            var socketState = new SocketState {workSocket = handler};
            handler.BeginReceive(socketState.buffer, 0, SocketState.BufferSize, 0, ReadCallback, socketState);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            var socketState = (SocketState) ar.AsyncState;
            var handler = socketState.workSocket;

            var bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                socketState.sb.Append(Encoding.GetString(socketState.buffer, 0, bytesRead));
                var content = socketState.sb.ToString();
                if (content.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                {
                    @out.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    Send(handler, content);
                }
                else
                {
                    handler.BeginReceive(socketState.buffer, 0, SocketState.BufferSize, 0, ReadCallback, socketState);
                }
            }
        }

        private static void Send(Socket handler, string data)
        {
            var byteData = Encoding.GetBytes(data);
            handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                var handler = (Socket) ar.AsyncState;
                var bytesSent = handler.EndSend(ar);
                @out.WriteLine("Sent {0} bytes to client.", bytesSent);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                @out.WriteLine(e.ToString());
            }
        }
    }
}