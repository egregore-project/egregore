using System.Net.Sockets;
using System.Text;

namespace egregore.Network
{
    public class SocketState
    {  
        public const int BufferSize = 1024;  

        public Socket Handler { get; set; }
        public byte[] buffer = new byte[BufferSize];  
        public StringBuilder sb = new StringBuilder();  
    }
}
