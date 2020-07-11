using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;

namespace egregore.Network
{
    // Current code is based on: https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example

    public sealed class SocketServer
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        public static ManualResetEvent signal = new ManualResetEvent(false);
        public static TextWriter @out;
        private static Task _task;
        
        public static void Start(string hostNameOrIpAddress, int port, CancellationToken cancellationToken = default)
        {
            var ipHostInfo = Dns.GetHostEntry(hostNameOrIpAddress);
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, port);

            _task = Task.Run(() => { 
                var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (true)
                    {
                        signal.Reset();
                        @out.WriteInfoLine("Waiting for a connection...");
                        var ar = listener.BeginAccept(AcceptCallback, listener);
                        while (!signal.WaitOne(10) && !ar.IsCompleted)
                            cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    @out?.WriteInfoLine("Server thread was cancelled.");
                }
                catch (Exception e)
                {
                    @out?.WriteErrorLine(e.ToString());
                }
                finally
                {
                    @out?.WriteInfoLine("Closing server connection...");
                    listener.Close();
                    listener.Dispose();
                } }, cancellationToken);
        }

        public static void Stop()
        {
            while (!_task.IsCanceled && !_task.IsCompleted && !_task.IsFaulted)
                signal.WaitOne(10);
            _task?.Dispose();
        }
        
        public static void AcceptCallback(IAsyncResult ar)
        {
            signal.Set();
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
                    @out?.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
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
                @out?.WriteLine("Sent {0} bytes to client.", bytesSent);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                @out?.WriteErrorLine(e.ToString());
            }
        }
    }
}