// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
using System.Net;
namespace vilark;

class WebListener
{
    private HttpListener m_listener;
    private string m_url;
    private HttpListenerContext? m_context = null;

    public string GetUrl() => m_url;

    public WebListener() {
        for (int port = 61980; port < 63000; port++) {
            string url = $"http://localhost:{port}/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            try {
                listener.Start();
            } catch (Exception e) {
                Log.Info($"Error: {e}");
                continue;
            }

            Log.Info($"Listening OK on {url}");
            m_listener = listener;
            m_url = url;
            break;
        }
        if (m_listener == null || m_url == null) {
            throw new Exception("Unable to find free HTTP port");
        }
    }

    public string GetRequest() {
        Log.Info("Waiting for request...");
        // Note: The GetContext method blocks while waiting for a request.
        m_context = m_listener.GetContext();

        // Todo: parse request if there are different types
        HttpListenerRequest request = m_context.Request;

        return "getfile";  // only one request type for now
    }

    public void SendResponse(string responseString) {
        Trace.Assert(m_context != null);

        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);

        HttpListenerResponse response = m_context.Response;
        response.ContentLength64 = responseBytes.Length;
        Stream output = response.OutputStream;
        output.Write(responseBytes, 0, responseBytes.Length);
        // You must close the output stream.
        output.Close();
        m_context = null;
    }

}
