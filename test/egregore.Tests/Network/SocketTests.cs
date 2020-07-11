using System;
using System.Threading;
using System.Threading.Tasks;
using egregore.Network;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Network
{
    public class SocketTests
    {
        private readonly ITestOutputHelper _console;

        public SocketTests(ITestOutputHelper console)
        {
            _console = console;
        }

        [Fact]
        public void Can_send_and_receive_from_socket()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);
            var cancel = new CancellationTokenSource();

            using var server = new SocketServer(@out);
            server.Start("localhost", 11000, cancel.Token);

            var client = new SocketClient(@out);
            client.ConnectAndSend("localhost", 11000);
            client.ConnectAndSend("localhost", 11000);

            cancel.Cancel();
        }
    }
}