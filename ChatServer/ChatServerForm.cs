using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using static ChatServer.ChatServerForm;

public class Message(int sender, int recipient, string content)
{
    public int SenderID { get; set; } = sender;
    public int ReceiverID { get; set; } = recipient;
    public string Content { get; set; } = content;
}

public class RESTAPIManager
{
    string create_edit_api_url = "https://localhost:8080/api/Client/CreateEdit";
    string delete_all_api_url = "https://localhost:8080/api/Client/DeleteAll";
    HttpClient rest_api_client = new();

    public async void UploadClienttoRESTAPI(ClientManager client_manager)
    {
        var data = new { id = 0, port = client_manager.connected_port };
        var Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await rest_api_client.PostAsync(create_edit_api_url, Content);

        string http_request_type = "Add client to server";
        CheckResponse(response, http_request_type);
    }

    public void DeleteAllClients()
    {
        var data = new { id = 0, port = 0 };
        var Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = rest_api_client.DeleteAsync(delete_all_api_url).Result;

        string http_request_type = "Delete all clients from server";
        CheckResponse(response, http_request_type);
    }

    public void CheckResponse(HttpResponseMessage response, string http_request_type)
    {
        string response_message;
        if (response.IsSuccessStatusCode)
        {
            response_message = $"Request : {http_request_type} was successful";
        }
        else
        {
            response_message = $"Request : {http_request_type} returned the following error \n {response.StatusCode}";

        }

        UpdateOpenChatTextBox(response_message);
    }

}

public class ServerManager()
{
    public RESTAPIManager rest_api_manager = new();

    private static Dictionary<int, ClientManager> connected_port_client_manager_pairs = [];

    TcpListener server_socket;

    bool isAcceptingClientConnection = false;
    Thread m_accept_client_connection_th;

    IPAddress m_ip_address;
    private bool isIPAddressSet = false;

    int m_port;
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

    private bool assertIPAddressAndPort()
    {
        return (isIPAddressSet && isPortSet);
    }

    private void AcceptClientConnection()
    {
        if (!assertIPAddressAndPort())
        {
            UpdateOpenChatTextBox($"IP Address status : {isIPAddressSet} Port status : {isPortSet}");
            return;
        }

        ClientManager client_manager;
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
            // rest_api_manager.UploadClienttoRESTAPI(client_manager);

            connected_port_client_manager_pairs.Add(client_manager.connected_port, client_manager);

            string welcome_msg = "Welcome to the chat server, Client " + client_manager.connected_port + "!";
            string message_to_send = ConvertMessageToJsonString(m_port, client_manager.connected_port, welcome_msg);
            client_manager.q_messages_to_send.Enqueue(message_to_send);

            client_manager.SetStreamReader();
            client_manager.StartReceivingMessage();
            client_manager.StartSendingMessage();
        }
    }

    private ClientManager GetClientToReceiveFromMessage(string message)
    {
        Message received_message = ParseJsonMessage(message);

        return connected_port_client_manager_pairs[received_message.ReceiverID];
    }

    private ClientManager GetSenderClientFromMessage(string message)
    {
        Message received_message = ParseJsonMessage(message);

        return connected_port_client_manager_pairs[received_message.SenderID];
    }

    private string GetContentFromMessage(string message)
    {
        Message received_message = ParseJsonMessage(message);

        return received_message.Content;
    }

    public void StartProcessingMessage(ClientManager client_manager)
    {
        client_manager.isProcessingMessage = true;
        client_manager.process_message_th = new Thread(() => ProcessMessage(client_manager));
        client_manager.process_message_th.Start();
    }

    public void ProcessMessage(ClientManager client_manager)
    {
        while (client_manager.isProcessingMessage)
        {
            if (client_manager.q_received_messages.TryDequeue(out string message_received))
            {
                string content = GetContentFromMessage(message_received);

                if (content == "") return;
                else if (content == "DISCONNECT")
                {
                    ClientManager sender_client = GetSenderClientFromMessage(message_received);
                    sender_client.DisconnectClient();
                }
                else
                {
                    ClientManager client_to_receive = GetClientToReceiveFromMessage(message_received);
                    client_to_receive.q_messages_to_send.Enqueue(message_received);
                }
            }
            else
            {
                Thread.Sleep(100);
            }
        }
    }

}

public class ClientManager
{
    static TcpClient client_socket;
    StreamReader reader;
    public int connected_port;
    NetworkStream network_stream;

    private bool isReceivingMessage = false;
    Thread receive_message_th;
    public ConcurrentQueue<string> q_received_messages = new();

    private bool isSendingMessages = false;
    Thread send_message_th;
    public ConcurrentQueue<string> q_messages_to_send = new();

    public bool isProcessingMessage = false;
    public Thread process_message_th;

    public ClientManager(TcpClient new_client_socket)
    {
        client_socket = new_client_socket;
        IPEndPoint temp_client_end_point = (IPEndPoint)client_socket.Client.RemoteEndPoint;
        connected_port = temp_client_end_point.Port;

        network_stream = client_socket.GetStream();
    }

    public void DisconnectClient()
    {
        client_socket.Close();
    }

    public void StartSendingMessage()
    {
        isSendingMessages = true;
        send_message_th = new(() => SendMessageToClient());
        send_message_th.Start();
    }

    public void SendMessageToClient()
    {
        while (isSendingMessages)
        {
            if (q_messages_to_send.TryDequeue(out string message_to_send))
            {
                byte[] bytes_to_send = Encoding.ASCII.GetBytes(message_to_send);
                network_stream.Write(bytes_to_send, 0, bytes_to_send.Length);
                network_stream.Flush();
            }
            else
            {
                Thread.Sleep(100);
            }
        }
    }

    public void SetStreamReader()
    {
        reader = new StreamReader(client_socket.GetStream());
    }

    public void StartReceivingMessage()
    {
        isReceivingMessage = true;
        receive_message_th = new(() => ReceiveMessage());
        receive_message_th.Start();
    }

    public async void ReceiveMessage()
    {

        while (isReceivingMessage)
        {
            string message = await reader.ReadLineAsync();
            if (message != "")
            {
                q_received_messages.Enqueue(message);
            }
        }
    }
}

namespace ChatServer
{

    public partial class ChatServerForm : Form
    {
        public const int WSA_CANCEL_BLOCKING_CALL_ERROR_CODE = 10004;
        public ServerManager server_manager;


        public ChatServerForm()
        {
            InitializeComponent();
        }

        static public void UpdateOpenChatTextBox(string message)
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

        static public string ConvertMessageToJsonString(int sender, int recipient, string message_text)
        {
            Message message = new Message(sender, recipient, message_text);

            return JsonConvert.SerializeObject(message);
        }


        static public Message ParseJsonMessage(string json)
        {
            return JsonConvert.DeserializeObject<Message>(json);
        }


        private void ServerStartButton_Click(object sender, EventArgs e)
        {
            server_manager = new ServerManager();
            // server_manager.rest_api_manager.DeleteAllClients();

            server_manager.StartChatServer();
        }

        private void ServerStopButton_Click(object sender, EventArgs e)
        {
        }


        private void ChatServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

    }
}
