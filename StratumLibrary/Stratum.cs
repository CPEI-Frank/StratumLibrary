using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;

using System.Text;
using System.Collections.Generic;

namespace Stratum
{
    public class Stratum
    {
        private Socket client;
        private Dictionary<string, string> responses = new Dictionary<string, string>();
        ManualResetEvent gotResponse = new ManualResetEvent(false);

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
                Socket arClient = (Socket)ar.AsyncState;

                // Complete the connection
                arClient.EndConnect(ar);

                // Signal that connection has been established
                connectDone.Set();
            };

            // Connect to the remote device
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);

            // Wait for signal
            connectDone.WaitOne();

            // Start receive handler
            Receiver(client);
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
                Socket arClient = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device
                int bytesSent = arClient.EndSend(ar);

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
            // Serialize stratumReq into JSON string
            var reqJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stratumReq) + '\n';
            var reqId = (string) stratumReq.Id;

            // Send JSON data to the remote device.
            Send(client, reqJSON);

            // Wait for response
            gotResponse.WaitOne();

            // Deserialize the response
            string strResponse = responses[reqId];
            StratumResponse<T> responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<StratumResponse<T>>(strResponse);
            responses.Remove(reqId);

            // Reset the state
            gotResponse.Reset();

            if (object.ReferenceEquals(null, responseObj))
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

            return responseObj;
        }

        private void Receiver(Socket client)
        {
            // Create the reading state object.
            StratumReadState state = new StratumReadState(client);

            Action<IAsyncResult> ReceiveCallback = null;
            ReceiveCallback = (IAsyncResult ar) =>
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StratumReadState arStatus = (StratumReadState)ar.AsyncState;
                Socket arClient = arStatus.workSocket;

                // Read data from the remote device.
                int bytesRead = arClient.EndReceive(ar);

                if (bytesRead <= 0)
                    return;

                lock (arStatus.sb)
                {
                    // There might be more data, so store the data received so far.
                    arStatus.sb.Append(Encoding.ASCII.GetString(arStatus.buffer, 0, bytesRead));

                    if (arStatus.buffer[bytesRead - 1] == '\n')
                    {
                        string strMessage = arStatus.sb.ToString();
                        arStatus.sb.Clear();

                        try
                        {
                            JObject jo = Newtonsoft.Json.JsonConvert.DeserializeObject(strMessage) as JObject;
                            string requestId = (string)jo["id"];

                            if (!String.IsNullOrEmpty(requestId))
                            {
                                responses.Add(requestId, strMessage);

                                gotResponse.Set();
                            }
                            else
                            {
                                // TODO: notifications handling
                                Console.WriteLine("Notification: {0}", strMessage);
                            }
                        }
                        catch (Newtonsoft.Json.JsonSerializationException e)
                        {
                            // TODO: handle parse error
                        }
                    }
                }

                arClient.BeginReceive(arStatus.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), arStatus);
            };

            client.BeginReceive(state.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }
    }

}