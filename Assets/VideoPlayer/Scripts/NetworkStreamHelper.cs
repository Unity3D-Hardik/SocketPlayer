using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace com.rambla.vr {

    [Serializable]
    public class JsonResponse
    {
        public string Path;  // video name
        public string PlayerState; // 0 = play , 1 = stop 
        public string CurrentTime; // Current play time 
        public string Duration;
        public string PlaybackSpeed;

    }

    public class NetworkStreamHelper : MonoBehaviour
    {

        Thread _thread;
        public int connectionPort = 23554;
        TcpListener _server;
        TcpClient _client;
        bool _isRunning;
        NetworkStream _networkStream;

        private CancellationTokenSource _cts;
        private int _timeoutInSeconds = 3;

        DateTime lastReceived;

        public delegate void OnDataReceived(JsonResponse response);
        public static event OnDataReceived OnDataUpdate;

        public string ipAddress = "127.0.0.1";


        // Start is called before the first frame update
        void Start()
        {
            ipAddress = IPManager.GetIP(ADDRESSFAM.IPv4);

            Debug.Log("Local IP :- "+ipAddress);

            // Receive on a separate thread so Unity doesn't freeze waiting for data
            ThreadStart ts = new ThreadStart(GetData);
            _thread = new Thread(ts);
            _thread.Start();
        }

        private void OnDisable()
        {
            _cts.Cancel();
            _server.Stop();
        }

        void GetData()
        {
            try
            {

                Debug.Log("Local IP :- " + ipAddress);
                IPAddress localIPAddress = IPAddress.Parse(ipAddress);

                Debug.Log("Local IP :- 1 " + ipAddress +" "+connectionPort  + localIPAddress.ToString());
                // Create the server
                _server = new TcpListener(localIPAddress, connectionPort);
                Debug.Log("Local IP :- 1.1 " + ipAddress + " " + connectionPort);
                _server.Start();


                Debug.Log("Local IP :- 2" + ipAddress);

                // Create a client to get the data stream
                _client = _server.AcceptTcpClient();

                Debug.Log("Local IP :- 3" + ipAddress);

                _cts = new CancellationTokenSource();

                Debug.Log("Local IP :- 4" + ipAddress);

                // Read data from the network stream
                _networkStream = _client.GetStream();

                Debug.Log("Local IP :- 5" + ipAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError("Server Close Exeption :- ");
                Debug.LogError(ex.ToString());
                _isRunning = false;
                _server.Stop();
                if (_cts != null)
                    _cts.Cancel();
            }

            // Start listening
            _isRunning = true;
            while (_isRunning)
            {
                Connection();
            }

        }

        void Connection()
        {

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    byte[] buffer = new byte[_client.ReceiveBufferSize];
                    int bytesRead = _networkStream.Read(buffer, 0, _client.ReceiveBufferSize);

                    // Decode the bytes into a string
                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Make sure we're not getting an empty string

                    if (bytesRead == 0)
                    {
                        if ((DateTime.Now - lastReceived).TotalSeconds > 10)
                        {
                            Console.WriteLine("No data received for 10 seconds. Restarting server...");
                            // RestartServer();
                            break;
                        }

                    }
                    else
                    {
                        lastReceived = DateTime.Now;
                        dataReceived.Trim();
                        if (!string.IsNullOrEmpty(dataReceived))
                        {

                            if (string.IsNullOrWhiteSpace(dataReceived))
                                return;

                            Debug.Log("Response -:- " + dataReceived);
                            JsonResponse result = Parse(dataReceived);
                            Debug.Log("Class Data :- " + JsonUtility.ToJson(result));

                            if (result != null)
                                OnDataUpdate?.Invoke(result);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Debug.Log("Server stopped due to timeout or cancellation.");

                    if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        Debug.Log("Connection forcibly closed by remote host.");
                        // Implement retry logic or other handling
                    }
                    else
                    {
                        Debug.Log("Socket exception occurred: " + ex.Message);
                    }
                    Debug.Log("Server stopped due to timeout or cancellation.");
                }
            }
        }

        public JsonResponse Parse(string response)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                JsonResponse result = new JsonResponse();
                result.Path = data.ContainsKey("path") ? data["path"].ToString() : "";
                result.PlayerState = data.ContainsKey("playerState") ? data["playerState"].ToString() : "";
                result.CurrentTime = data.ContainsKey("currentTime") ? data["currentTime"].ToString() : string.Empty;
                result.Duration = data.ContainsKey("duration") ? data["duration"].ToString() : "";
                result.PlaybackSpeed = data.ContainsKey("playbackSpeed") ? data["playbackSpeed"].ToString() : "";

                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error parsing JSON: " + ex.Message);
                return null; // Or handle the error in a different way
            }
        }

        public void FindLocalIPAddress()
        {
            try
            {
                string localIP = GetLocalIPAddress();
                Debug.Log("Local IP address: " + localIP);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error getting local IP address: " + ex.Message);
            }
        }

       /* private string GetLocalIPAddress()
        {
            var host = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(host);

            foreach (var ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
        }*/

        private string GetLocalIPAddress()
        {
            string ipAddress;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus
             == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                        {
                            Debug.Log("IP Address : - "+ip.Address.ToString());
                            ipAddress = ip.Address.ToString();
                            return ipAddress;
                        }
                    }
                }
            }
            return null;
        }
    }
}