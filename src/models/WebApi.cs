// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
using System.Net.Sockets;
namespace vilark;

class WebListener
{
    private OutputModel outputModel;
    private EventQueue<Notification> m_notifications;
    private Socket? m_socket = null;
    private Socket? m_client_socket = null;
    private string? m_path = null;
    private byte[] m_buffer = new byte[1024];
    private EventQueue<string> m_web_replies = new();

    public WebListener(OutputModel outputModel, EventQueue<Notification> notifications) {
        this.outputModel = outputModel;
        m_notifications = notifications;
    }

    public void Start() {
        Trace.Assert(m_socket == null);
        Trace.Assert(m_path == null);
        if (Environment.GetEnvironmentVariable("VILARK_IPC_URL") != null) {
            Log.Info("VILARK_IPC_URL already set, not starting another web listener");
            return;
        }

        // Don't start socket for the buffer selector
        if (outputModel.GetEditorCommand() == null) {
            Log.Info("No editor command set, not starting web listener");
            return;
        }

        m_path  = $"/tmp/vilark.sock.{Environment.ProcessId}";
        Log.Info($"Listening on socket {m_path}");
        m_socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        m_socket.Bind(new UnixDomainSocketEndPoint(m_path));
        m_socket.Listen(5);

        Environment.SetEnvironmentVariable("VILARK_IPC_URL", m_path);

        var thread = new Thread(() => {
                try {
                    while (true) {
                        string request = GetRequest();
                        m_notifications.AddEvent(new Notification(WebRequest: request));
                        m_web_replies.ConsumerWaitHandle.WaitOne();
                        var response = m_web_replies.TakeEvent();
                        SendResponse(response);
                    }
                } catch (Exception e) {
                    m_notifications.AddEvent(new Notification(FatalErrorMessage: e.ToString()));
                }
            });
        thread.Name = "ApiThread";
        thread.Start();
    }

    public void AddResponse(string response) {
        m_web_replies.AddEvent(response);
    }

    private string GetRequest() {
        Trace.Assert(m_socket != null);
        Log.Info("Waiting for request...");
        m_client_socket = m_socket.Accept();
        using (var memStream = new MemoryStream(100)) {
            while (true) {
                var numberOfBytesReceived = m_client_socket.Receive(m_buffer,
                        0, m_buffer.Length, SocketFlags.None);
                memStream.Write(m_buffer, 0, numberOfBytesReceived);
                if (memStream.Length > 0 && memStream.GetBuffer()[memStream.Length-1] == '\n') {
                    break;
                }
            }
        }
        Log.Info("Got request");
        return "getfile";  // only one request type for now
    }

    private void SendResponse(string responseString) {
        Log.Info("SendResponse()");
        Trace.Assert(m_client_socket != null);
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
        int totalSent = 0;
        while (totalSent != responseBytes.Length) {
            int batch = m_client_socket.Send(responseBytes,
                    totalSent,
                    responseBytes.Length - totalSent,
                    SocketFlags.None);
            totalSent += batch;
        }
        m_client_socket.Close();
        m_client_socket = null;
    }

    public void CleanupSocket() {
        if (m_path != null && File.Exists(m_path)) {
            Log.Info($"Removing socket path {m_path}");
            File.Delete(m_path);
        }
    }

}
