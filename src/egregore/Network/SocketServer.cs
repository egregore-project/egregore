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
    public sealed class SocketServer : IDisposable
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly ManualResetEvent _signal = new ManualResetEvent(false);
        private readonly TextWriter _out;

        private Task _task;
        private CancellationTokenSource _source;
        private readonly string _id;

        public SocketServer(string id = default, TextWriter @out = default)
        {
            _id = id ?? "[SERVER]";
            _out = @out;
        }
        
        public void Start(string hostNameOrIpAddress, int port, CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default)
            {
                _source = new CancellationTokenSource();
                cancellationToken = _source.Token;
            }

            var ipHostInfo = Dns.GetHostEntry(hostNameOrIpAddress);
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, port);
            _task = Task.Run(() => { AcceptConnection(ipAddress, localEndPoint, cancellationToken); }, cancellationToken);
        }

        private void AcceptConnection(IPAddress address, EndPoint endpoint, CancellationToken cancellationToken)
        {
            var listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Bind(endpoint);
                listener.Listen(100);

                while (true)
                {
                    _signal.Reset();
                    _out?.WriteInfoLine($"{_id}: Waiting for a connection...");
                    var ar = listener.BeginAccept(AcceptCallback, listener);
                    while (!_signal.WaitOne(10) && !ar.IsCompleted)
                        cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                _out?.WriteInfoLine($"{_id}: Server thread was cancelled.");
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
            finally
            {
                _out?.WriteInfo($"{_id}: Closing connection... ");
                listener.Close();
                listener.Dispose();
                _out?.WriteInfoLine("done.");
            }
        }
        
        public void AcceptCallback(IAsyncResult ar)
        {
            _signal.Set();
            var listener = (Socket) ar.AsyncState;
            var socket = listener.EndAccept(ar);
            _out?.WriteInfoLine($"{_id}: Connected to {socket.RemoteEndPoint}");
            var socketState = new SocketState {Handler = socket};
            socket.BeginReceive(socketState.buffer, 0, SocketState.BufferSize, 0, ReadCallback, socketState);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            var socketState = (SocketState) ar.AsyncState;
            var handler = socketState.Handler;

            var bytesRead = handler.EndReceive(ar);
            if (bytesRead <= 0)
                return;

            socketState.sb.Append(_encoding.GetString(socketState.buffer, 0, bytesRead));
            var message = socketState.sb.ToString();
            if (message.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
            {
                _out?.WriteLine($"{_id}: Read {bytesRead} bytes from socket. Message: '{message}'");
                SendMessage(handler, message);
            }
            else
            {
                handler.BeginReceive(socketState.buffer, 0, SocketState.BufferSize, 0, ReadCallback, socketState);
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
                var handler = (Socket) ar.AsyncState;
                var sent = handler.EndSend(ar);
                _out?.WriteInfoLine($"{_id}: Sent {sent} bytes to client.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
            }
        }

        public void Dispose()
        {
            _source?.Cancel(true);
            while (!_task.IsCanceled && !_task.IsCompleted && !_task.IsFaulted)
                _signal.WaitOne(10);
            _signal?.Dispose();
            _task?.Dispose();
        }
    }
}