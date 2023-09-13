// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Net;
namespace vilark;

class WebListener
{
    private Controller m_controller;
    private HttpListener m_listener;
    private string m_url;

    public string GetUrl() => m_url;

    public WebListener(Controller c) {
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
        m_controller = c;
        if (m_listener == null || m_url == null) {
            throw new Exception("Unable to find free HTTP port");
        }
    }

    public void Run() {
        Log.Info("Waiting for requests...");
        while (true) {
            DoRequest(m_listener);
        }
    }

    private void DoRequest(HttpListener listener) {
        // Note: The GetContext method blocks while waiting for a request.
        HttpListenerContext context = listener.GetContext();

        // Todo: parse request if there are different types
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        // Tell the main thread
        m_controller.m_notifications.AddEvent(new Notification(WebRequest: "getfile"));

        // Wait for the user to make a choice
        m_controller.m_web_replies.ConsumerWaitHandle.WaitOne();
        var choice = m_controller.m_web_replies.TakeEvent();

        // Construct a response.
        string responseString = $"{choice}";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        // You must close the output stream.
        output.Close();
    }
}
