using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using static ChatServer.ChatServerForm;


namespace ChatServer
{ 

    public partial class ChatServerForm : Form
    {
        public const int WSA_CANCEL_BLOCKING_CALL_ERROR_CODE = 10004;

        public class RESTAPIManager()
        {
            string create_edit_api_url = "https://localhost:8080/api/Client/CreateEdit";
            HttpClient rest_api_client = new();

            public async void UploadClienttoRESTAPI(ClientManager client_manager)
            {
                var data = new { id = 0, port = client_manager.connected_port};
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await rest_api_client.PostAsync(create_edit_api_url, content);
                CheckResponse(response);
            }

            public void CheckResponse(HttpResponseMessage response)
            {
                if (response.IsSuccessStatusCode)
                {
                    UpdateOpenChatTextBox("Client added to server");
                }
                else
                {
                    UpdateOpenChatTextBox($"Error: {response.StatusCode}");
                }
            }

        }

        public class ServerManager()
        {
            ClientManager client_manager;
            RESTAPIManager rest_api_manager;

            TcpListener server_socket;
            bool isAcceptingClientConnection = false;
            Thread m_accept_client_connection_th;
            IPAddress m_ip_address;
            int m_port;

            private bool isIPAddressSet = false;
            private bool isPortSet = false;


            public void StartAcceptingClientConnection()
            {
                isAcceptingClientConnection = true;
            }

            public void StopAcceptingClientConnection()
            {
                isAcceptingClientConnection = false;
            }

            public void SetIPAddress()
            {
                if (IPAddressTextBox.Text == "")
                {
                    IPAddressTextBox.Text = "127.0.0.1";
                }

                m_ip_address = IPAddress.Parse(IPAddressTextBox.Text);

                isIPAddressSet = true;
            }

            public void SetPort()
            {
                if (PortTextBox.Text == "")
                {
                    PortTextBox.Text = "8888";
                }

                m_port = int.Parse(PortTextBox.Text);

                isPortSet = true;
            }

            public void StartChatServer()
            {
                UpdateOpenChatTextBox("Starting chat server.");
                SetIPAddress();
                SetPort();

                isAcceptingClientConnection = true;
                
                server_socket = new(m_ip_address, m_port);
                server_socket.Start();

                m_accept_client_connection_th = new(AcceptClientConnection);
                m_accept_client_connection_th.Start();
            }

            private async void AcceptClientConnection()
            {
                while (isAcceptingClientConnection)
                {
                    try
                    {
                        TcpClient new_client_socket = server_socket.AcceptTcpClient();
                        client_manager = new(new_client_socket);
                    }
                    catch (SocketException ex) when (ex.ErrorCode == WSA_CANCEL_BLOCKING_CALL_ERROR_CODE) // 10004
                    {
                        UpdateOpenChatTextBox("Pending connection handled.");
                        break;
                    }

                    UpdateOpenChatTextBox($"Client connected on port {client_manager.connected_port}");
                    rest_api_manager.UploadClienttoRESTAPI(client_manager);

                    client_list.Add(client_manager);

                    string welcome_msg = "Welcome to the chat server, Client " + client_manager.connected_port + "!";
                    string message_to_send = CreateJsonMessage(m_port, client_manager.connected_port, welcome_msg);
                    client_manager.SendMessageToClient(message_to_send);

                    client_manager.SetStreamReader();
                    client_manager.StartHandlingCommunication();
                }
            }

        }
        public class ClientManager
        {
            static TcpClient client_socket;
            StreamReader reader;
            public int connected_port;
            private bool isHandlingCommunication = false;
            NetworkStream network_stream = client_socket.GetStream();
            Thread handle_communucation_th;

            public ClientManager(TcpClient new_client_socket)
            {
                client_socket = new_client_socket;
                IPEndPoint temp_client_end_point = (IPEndPoint)client_socket.Client.RemoteEndPoint;
                connected_port = temp_client_end_point.Port; 
            }

            public void DisconnectConnection()
            {
                client_socket.Close();
            }

            public void SendMessageToClient(string message)
            {
                byte[] bytes_to_send = Encoding.ASCII.GetBytes(message);
                network_stream.Write(bytes_to_send, 0, bytes_to_send.Length);
                network_stream.Flush();
            }

            public void SetStreamReader()
            {
                reader = new StreamReader(client_socket.GetStream());
            }

            public void StartHandlingCommunication()
            {
                isHandlingCommunication = true;
                handle_communucation_th = new(() => HandleCommunication());
            }

            public async void HandleCommunication()
            {

                while (isHandlingCommunication)
                {
                    string message = "";
                    if (client_socket.Connected)
                    {
                        message = await reader.ReadLineAsync();
                    }
                    else
                    {
                        UpdateOpenChatTextBox("");
                    }
                    if (message != "")
                    {
                        Message received_message = ParseJsonMessage(message);
                        string message_to_send = received_message.content;
                        if (message_to_send == "DISCONNECT")
                        {
                            TcpClient recipient_client = client_list[received_message.receiver_id];
                            recipient_client.Close();
                            client_list.Remove(received_message.receiver_id);
                            UpdateOpenChatTextBox("Client " + received_message.sender_id + " disconnected");
                        }

                        UpdateOpenChatTextBox("Message from : " + received_message.sender_id);
                        if (client_list.ContainsKey((received_message.receiver_id)))
                        {
                            TcpClient recipient_client = client_list[received_message.receiver_id];
                            SendMessageToClient(recipient_client, message);
                            UpdateOpenChatTextBox("Message forwarded to " + received_message.receiver_id);
                        }
                        else
                        {
                            UpdateOpenChatTextBox("Recipient " + received_message.receiver_id + " not found.");
                        }
                    }
                }
            }
        }

        private static List<ClientManager> client_list = [];
        private readonly HttpClient httpClient = new HttpClient();


        public class Message(int sender, int recipient, string content)
        {
            public int sender_id { get; set; } = sender;
            public int receiver_id { get; set; } = recipient;
            public string? content { get; set; } = content;
        }

        public ChatServerForm()
        {
            InitializeComponent();
        }

        static private void UpdateOpenChatTextBox(string message)
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
            Message message = new Message(sender, recipient, message_text);

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
                else
                {
                    UpdateOpenChatTextBox("");
                }
                if (message != "")
                {
                    Message received_message = ParseJsonMessage(message);
                    string message_to_send = received_message.content;
                    if (message_to_send == "DISCONNECT")
                    {
                        TcpClient recipient_client = client_list[received_message.receiver_id];
                        recipient_client.Close();
                        client_list.Remove(received_message.receiver_id);
                        UpdateOpenChatTextBox("Client " + received_message.sender_id + " disconnected");
                    }

                    UpdateOpenChatTextBox("Message from : " + received_message.sender_id);
                    if (client_list.ContainsKey((received_message.receiver_id)))
                    {
                        TcpClient recipient_client = client_list[received_message.receiver_id];
                        SendMessageToClient(recipient_client, message);
                        UpdateOpenChatTextBox("Message forwarded to " + received_message.receiver_id);
                    }
                    else
                    {
                        UpdateOpenChatTextBox("Recipient " + received_message.receiver_id + " not found.");
                    }
                }
            }
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

            isAcceptingClientConnection = true;
            int port = int.Parse(PortTextBox.Text);
            server_socket = new(ip_address, port);
            m_client_th = new(ClientConnectionThread);
            m_client_th.Start();

        }

        private void ServerStopButton_Click(object sender, EventArgs e)
        {
            UpdateOpenChatTextBox("Stopping chat server.");
            DisconnectAllClients();
            isAcceptingClientConnection = false;
            m_client_th.Join();
            server_socket.Stop();
            DeleteAllApi();
        }

        private void ChatServerForm_Load(object sender, EventArgs e)
        {

        }

        private void ChatServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectAllClients();
            isAcceptingClientConnection = false;
            m_client_th.Join();
            server_socket.Stop();
            DeleteAllApi();
        }

    }
}
