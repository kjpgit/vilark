// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
using System.Net.Sockets;
namespace vilark;

class WebListener
{
    private Socket m_socket;
    private Socket? m_client_socket = null;
    private string m_url;
    private byte[] m_buffer = new byte[1024];

    public string GetUrl() => m_url;

    public WebListener() {
        var path = $"/tmp/vilark.sock.{Environment.ProcessId}";
        m_url = path;
        m_socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        m_socket.Bind(new UnixDomainSocketEndPoint(path));
        m_socket.Listen(5);
    }

    public string GetRequest() {
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
        return "getfile";  // only one request type for now
    }

    public void SendResponse(string responseString) {
        Trace.Assert(m_client_socket != null);
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
        int totalSent = 0;
        while (totalSent != responseBytes.Length) {
            int batch = m_client_socket!.Send(responseBytes,
                    totalSent,
                    responseBytes.Length - totalSent,
                    SocketFlags.None);
            totalSent += batch;
        }
        m_client_socket!.Close();
        m_client_socket = null;
    }

}
