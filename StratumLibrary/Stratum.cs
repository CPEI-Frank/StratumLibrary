using System;
using System.Net;
using Newtonsoft.Json.Linq;

using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace Stratum
{
    public class Stratum
    {
        private Socket client;

        /// <summary>
        /// Constructor of Stratum interface class
        /// </summary>
        /// <param name="ipAddress">IPv4 address</param>
        /// <param name="port">Port number</param>
        public Stratum(string ipAddress, int port)
        {
            // End point for the remote device
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);

            // Create TCP socket
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect done event
            ManualResetEvent connectDone = new ManualResetEvent(false);

            Action<IAsyncResult> ConnectCallback = null;
            ConnectCallback = (IAsyncResult ar) =>
            {
                // Retrieve socket from the state object
                Socket cli = (Socket)ar.AsyncState;

                // Complete the connection
                cli.EndConnect(ar);

                // Signal that connection has been established
                connectDone.Set();
            };

            // Connect to the remote device
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);

            // Wait for signal
            connectDone.WaitOne();
        }

        ~Stratum()
        {
            // Release the socket
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private void Send(Socket client, String data)
        {
            // Send done event
            ManualResetEvent sendDone = new ManualResetEvent(false);

            Action<IAsyncResult> SendCallback = null;
            SendCallback = (IAsyncResult ar) =>
            {
                // Retrieve the socket from the state object
                Socket cli = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device
                int bytesSent = cli.EndSend(ar);

                // Signal that all bytes have been sent
                sendDone.Set();
            };

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);

            // Wait for signal
            sendDone.WaitOne();
        }

        /// <summary>
        /// Invoke remote method
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="method">Method name</param>
        /// <param name="arg">Argument</param>
        /// <returns>StratumResponse object</returns>
        public StratumResponse<T> Invoke<T>(string method, object arg)
        {
            var req = new StratumRequest()
            {
                Method = method,
                Params = new object[] { arg }
            };
            return Invoke<T>(req);
        }

        /// <summary>
        /// Invoke remote method
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="method">Method name</param>
        /// <param name="args">Arguments array</param>
        /// <returns>StratumResponse object</returns>
        public StratumResponse<T> Invoke<T>(string method, object[] args)
        {
            var req = new StratumRequest()
            {
                Method = method,
                Params = args
            };
            return Invoke<T>(req);
        }

        private StratumResponse<T> Invoke<T>(StratumRequest stratumReq)
        {
            StratumResponse<T> rjson = null;

            // Serialize stratumReq into JSON string
            var reqJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stratumReq) + '\n';

            // Send JSON data to the remote device.
            Send(client, reqJSON);

            // Create the reading state object.
            StratumReadState state = new StratumReadState(client);

            // Receive event
            ManualResetEvent receiveDone = new ManualResetEvent(false);

            Action<IAsyncResult> ReceiveCallback = null;
            ReceiveCallback = (IAsyncResult ar) =>
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StratumReadState st = (StratumReadState)ar.AsyncState;
                Socket ci = st.workSocket;

                // Read data from the remote device.
                int bytesRead = ci.EndReceive(ar);

                if (bytesRead <= 0)
                    return;

                lock (st.sb)
                {
                    // There might be more data, so store the data received so far.
                    st.sb.Append(Encoding.ASCII.GetString(st.buffer, 0, bytesRead));

                    if (st.buffer[bytesRead - 1] != '\n')
                    {
                        //  No EOL at the end of buffer, going to get the rest of data
                        ci.BeginReceive(st.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), st);
                    }
                    else
                    {
                        string strResponse = st.sb.ToString();

                        // Deserialize the response
                        rjson = Newtonsoft.Json.JsonConvert.DeserializeObject<StratumResponse<T>>(strResponse);

                        if (rjson == null)
                        {
                            try
                            {
                                JObject jo = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse) as JObject;
                                throw new Exception(jo["Error"].ToString());
                            }
                            catch (Newtonsoft.Json.JsonSerializationException)
                            {
                                throw new Exception("Inconsistent or empty response");
                            }
                        }

                        // Signal that all bytes have been received.
                        receiveDone.Set();
                    }
                }
            };

            // Begin receiving the data from the remote device.
            client.BeginReceive(state.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            receiveDone.WaitOne();

            return rjson;
        }
    }
}