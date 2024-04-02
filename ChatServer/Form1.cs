using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Http;

namespace ChatServer
{

    public partial class ChatServerForm : Form
    {
        TcpListener server_socket;
        TcpClient? client_socket = default;
        Thread m_client_th;
        bool conn_thread_bool = false;

        private static Dictionary<int, TcpClient> client_list = [];
        private readonly HttpClient httpClient = new HttpClient();

        public class Message
        {
            public int sender_json { get; set; }
            public int recipient_json { get; set; }
            public string message_json { get; set; }
        }

        public ChatServerForm()
        {
            InitializeComponent();
        }

        private void UpdateOpenChatTextBox(string message)
        {
            if (OpenChatTextBox.InvokeRequired)
            {
                OpenChatTextBox.Invoke(new Action<string>(UpdateOpenChatTextBox), message);
            }
            else
            {
                message += "\r\n";
                OpenChatTextBox.Text += message;
            }
        }

        public string CreateJsonMessage(int sender, int recipient, string message_text)
        {
            Message message = new Message
            {
                sender_json = sender,
                recipient_json = recipient,
                message_json = message_text
            };

            return JsonConvert.SerializeObject(message);
        }


        public Message ParseJsonMessage(string json)
        {
            return JsonConvert.DeserializeObject<Message>(json);
        }

        private async Task HandleClientCommunication(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());

            while (true)
            {
                string message = "";
                if (client.Connected)
                {
                    message = await reader.ReadLineAsync();
                }
                if (message != "")
                {
                    Message received_message = ParseJsonMessage(message);
                    int sender_id = received_message.sender_json;
                    int recipient_id = received_message.recipient_json;
                    string message_to_send = received_message.message_json;
                    if (message_to_send == "DISCONNECT")
                    {
                        TcpClient recipient_client = client_list[recipient_id];
                        recipient_client.Close();
                        client_list.Remove(recipient_id);
                        UpdateOpenChatTextBox("Client " + sender_id + " disconnected");
                    }

                    UpdateOpenChatTextBox("Message from : " + sender_id);
                    if (client_list.ContainsKey((recipient_id)))
                    {
                        TcpClient recipient_client = client_list[recipient_id];
                        SendMessageToClient(recipient_client, message);
                        UpdateOpenChatTextBox("Message forwarded to " + recipient_id);
                    }
                    else
                    {
                        UpdateOpenChatTextBox("Recipient " + recipient_id + " not found.");
                    }
                }
            }
        }


        private void SendMessageToClient(TcpClient client, string message)
        {
            NetworkStream network_stream = client.GetStream();

            byte[] bytes_to_send = Encoding.ASCII.GetBytes(message);
            network_stream.Write(bytes_to_send, 0, bytes_to_send.Length);
            network_stream.Flush();
        }

        private void DisconnectAllClients()
        {
            int server_port = int.Parse(PortTextBox.Text);
            foreach (var client_pair in client_list)
            {
                try
                {
                    TcpClient client_socket = client_pair.Value;
                    NetworkStream network_stream = client_socket.GetStream();
                    IPEndPoint client_end_point = (IPEndPoint)client_socket.Client.RemoteEndPoint;

                    int client_port = client_end_point.Port;
                    string disconnect_msg = "DISCONNECT";
                    string message_to_send = CreateJsonMessage(server_port, client_port, disconnect_msg);
                    SendMessageToClient(client_socket, message_to_send);

                    client_socket.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error while disconnecting client: " + ex.Message);
                }
            }
            client_list.Clear();
        }

        private async void ClientConnectionThread()
        {
            server_socket.Start();
            int server_port = int.Parse(PortTextBox.Text);
            HttpClient client = new();
            while (conn_thread_bool)
            {
                try 
                {
                    client_socket = server_socket.AcceptTcpClient();
                }
                catch (SocketException ex) when (ex.ErrorCode == 10004) // WSACancelBlockingCall error code
                {
                    UpdateOpenChatTextBox("Pending connection handled.");
                    break;
                }
                IPEndPoint client_end_point = (IPEndPoint)client_socket.Client.RemoteEndPoint;
                int client_port = client_end_point.Port;
                UpdateOpenChatTextBox($"Client connected on port {client_port}");

                string api_url = "https://localhost:8080/api/Client/CreateEdit";
                var data = new { id = 0, port = client_port };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(api_url, content);

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    UpdateOpenChatTextBox("Client added to server");
                }
                else
                {
                    UpdateOpenChatTextBox($"Error: {response.StatusCode}");
                }

                client_list.Add(client_port, client_socket);

                string welcome_msg = "Welcome to the chat server, Client " + client_port + "!";
                string message_to_send = CreateJsonMessage(server_port, client_port, welcome_msg);
                SendMessageToClient(client_socket, message_to_send);


                Thread client_thread = new(() => HandleClientCommunication(client_socket));
                client_thread.Start();
            }
        }

        private void DeleteAllApi()
        {
            HttpClient client = new();
            string api_url = "https://localhost:8080/api/Client/DeleteAll";
            var data = new { id = 0, port = 0 };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.DeleteAsync(api_url).Result;

            // Check response status
            if (response.IsSuccessStatusCode)
            {
                UpdateOpenChatTextBox("Server API Initialized");
            }
            else
            {
                UpdateOpenChatTextBox($"Error: {response.StatusCode}");
            }
        }


        private void ServerStartButton_Click(object sender, EventArgs e)
        {
            DeleteAllApi();
            UpdateOpenChatTextBox("Starting chat server.");
            if (IPAddressTextBox.Text == "")
            {
                IPAddressTextBox.Text = "127.0.0.1";
            }
            IPAddress ip_address = IPAddress.Parse(IPAddressTextBox.Text);

            if (PortTextBox.Text == "")
            {
                PortTextBox.Text = "8888";
            }

            conn_thread_bool = true;
            int port = int.Parse(PortTextBox.Text);
            server_socket = new(ip_address, port);
            m_client_th = new(ClientConnectionThread);
            m_client_th.Start();

        }

        private void ServerStopButton_Click(object sender, EventArgs e)
        {
            UpdateOpenChatTextBox("Stopping chat server.");
            DisconnectAllClients();
            conn_thread_bool = false;
            m_client_th.Join();
            server_socket.Stop();
            DeleteAllApi();
        }
    }
}
