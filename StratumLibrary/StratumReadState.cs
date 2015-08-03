using System.Text;

using System.Net.Sockets;

namespace Stratum
{
    /// <summary>
    /// Represents internal state of Stratum interface
    /// </summary>
    class StratumReadState
    {
        public StratumReadState(Socket client)
        {
            workSocket = client;
        }

        /// <summary>
        /// Client socket
        /// </summary>
        public Socket workSocket = null;
        /// <summary>
        /// Receive buffer size
        /// </summary>
        public const int BufferSize = 8;
        /// <summary>
        /// Receive buffer
        /// </summary>
        public byte[] buffer = new byte[BufferSize];
        /// <summary>
        /// Received data string
        /// </summary>
        public StringBuilder sb = new StringBuilder();
    }
}
