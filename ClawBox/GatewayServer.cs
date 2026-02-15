using System;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ClawBox
{
    /// <summary>
    /// In-process server that listens on the OpenClaw gateway port (18789).
    /// Runs on Xbox/UWP without WSL or full-trust. For full OpenClaw gateway
    /// you need a Node.js UWP runtime (e.g. node-uwp-wrapper); this is a placeholder
    /// that demonstrates start/stop and uses ChakraCore as the JS runtime.
    /// </summary>
    public sealed class GatewayServer
    {
        public const int DefaultPort = 18789;
        private StreamSocketListener _listener;
        private bool _running;

        /// <summary>Fired for log messages: bind result, connection received, errors.</summary>
        public event Action<string> Log;

        public bool IsRunning => _running;

        public async Task StartAsync()
        {
            if (_running)
                return;

            Log?.Invoke("Creating socket listener…");
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += OnConnectionReceived;

            var host = new HostName("127.0.0.1");
            try
            {
                await _listener.BindEndpointAsync(host, DefaultPort.ToString());
                _running = true;
                Log?.Invoke("Server bound to 127.0.0.1:" + DefaultPort + ". Listening.");
            }
            catch (Exception ex)
            {
                _listener?.Dispose();
                _listener = null;
                Log?.Invoke("Bind failed: " + ex.Message);
                throw;
            }
        }

        public void Stop()
        {
            if (!_running)
                return;

            _listener?.Dispose();
            _listener = null;
            _running = false;
            Log?.Invoke("Server stopped.");
        }

        private async void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            string remote = args.Socket.Information.RemoteAddress?.DisplayName ?? "?";
            Log?.Invoke("Connection from " + remote);

            try
            {
                string body = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>ClawBox</title></head><body>" +
                    "<h1>ClawBox</h1><p>OpenClaw embedded, ChakraCore JS runtime.</p>" +
                    "<p>Server is listening on port " + DefaultPort + ".</p></body></html>";
                string response = "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: text/html; charset=utf-8\r\n" +
                    "Content-Length: " + System.Text.Encoding.UTF8.GetByteCount(body) + "\r\n" +
                    "Connection: close\r\n\r\n" + body;

                using (var outStream = args.Socket.OutputStream)
                using (var writer = new DataWriter(outStream))
                {
                    writer.WriteString(response);
                    await writer.StoreAsync();
                    await outStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke("Connection error: " + ex.Message);
            }
            finally
            {
                args.Socket.Dispose();
            }
        }
    }
}
