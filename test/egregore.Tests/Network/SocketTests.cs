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

        public SocketTests(ITestOutputHelper console)
        {
            _console = console;
        }

        [Fact]
        public async Task Can_send_and_receive_from_socket()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);

            SocketServer.@out = @out;
            SocketServer.@in = Console.In;
            SocketClient.@out = @out;

            _ = Task.Run(() => { SocketServer.StartListening("localhost", 11000); });

            await Task.Run(() =>
            {
                SocketClient.StartClient("localhost", 11000);
            });
        }
    }
}