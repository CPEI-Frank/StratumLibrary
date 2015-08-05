using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text;
using System.Collections.Generic;

namespace Stratum
{
    public class Stratum
    {
        private Socket client;

        object responsesLock = new object();
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
        /// <returns>StratumResponse object</returns>
        public StratumResponse<T> Invoke<T>(string method)
        {
            var req = new StratumRequest()
            {
                Method = method,
                Params = new object[] { }
            };
            return Invoke<T>(req);
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
            var reqJSON = JsonConvert.SerializeObject(stratumReq) + '\n';

            // Send JSON data to the remote device.
            Send(client, reqJSON);

            // Wait for response
            gotResponse.WaitOne();

            var strResponse = string.Empty;
            lock (responsesLock)
            {
                // Deserialize the response
                strResponse = responses[stratumReq.Id];
                responses.Remove(stratumReq.Id);
            }

            // Deserialize response into new instance of StratumResponse<T> 
            StratumResponse<T> responseObj = JsonConvert.DeserializeObject<StratumResponse<T>>(strResponse);

            // Reset the state
            gotResponse.Reset();

            if (responseObj == null)
            {
                try
                {
                    JObject jResponseObj = JsonConvert.DeserializeObject(strResponse) as JObject;
                    throw new Exception(jResponseObj["Error"].ToString());
                }
                catch (JsonSerializationException)
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
                        var strMessage = arStatus.sb.ToString();
                        arStatus.sb.Clear();

                        try
                        {
                            JObject jResponse = JsonConvert.DeserializeObject(strMessage) as JObject;
                            var reqId = (string)jResponse["id"];

                            if (!String.IsNullOrEmpty(reqId))
                            {
                                lock (responsesLock)
                                {
                                    responses.Add(reqId, strMessage);
                                }

                                gotResponse.Set();
                            }
                            else
                            {
                                StratumNotification jNotification = JsonConvert.DeserializeObject<StratumNotification>(strMessage);

                                var NotifyProcessThread = new Thread(() => NotificationHandler(jNotification.Method, jNotification.Params));
                                NotifyProcessThread.Start();
                            }
                        }
                        catch (JsonSerializationException e)
                        {
                            // TODO: handle parse error
                        }
                    }
                }

                arClient.BeginReceive(arStatus.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), arStatus);
            };

            client.BeginReceive(state.buffer, 0, StratumReadState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }

        /// <summary>
        /// Notifications stub which is run in a separate thread. If you wish to implement real notification processing then just override this method in the derived class.
        /// </summary>
        /// <param name="NotificationMethod">Method name</param>
        /// <param name="NotificationData">Array of values</param>
        private static void NotificationHandler(string NotificationMethod, JArray NotificationData)
        {
            Console.WriteLine("\nNotification: Method={0}, data={1}", NotificationMethod, NotificationData.ToString());
        }
    }

}