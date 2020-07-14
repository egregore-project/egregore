using System;
using System.Threading.Tasks;
using egregore.Network;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Network
{
    public class SocketTests
    {
        private readonly ITestOutputHelper _console;
        private readonly string _hostName;
        private readonly int _port;

        public SocketTests(ITestOutputHelper console)
        {
            _console = console;
            _hostName = "localhost";
            _port = 11000;
        }
        
        [Fact]
        public void Can_send_and_receive_from_socket()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);

            var protocol = new EchoProtocol(@out);
            using var server = new SocketServer(protocol, default, @out);
            server.Start(_hostName, _port);

            var client = new SocketClient(protocol, default, @out);
            client.ConnectAndSendTestMessage(_hostName, _port);

            client.Connect(_hostName, _port);
            client.SendTestMessage();
            client.Disconnect();

            client.Connect(_hostName, _port);
            client.SendTestMessage();
            client.Disconnect();
        }
    }
}