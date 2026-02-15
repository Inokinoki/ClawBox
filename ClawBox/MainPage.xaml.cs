using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.ChakraCore;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ClawBox
{
    public sealed partial class MainPage : Page
    {
        private readonly GatewayServer _server = new GatewayServer();
        private string _embeddedOpenClawVersion = "1.0.0";
        private readonly List<string> _logLines = new List<string>();
        private readonly List<string> _serverLogLines = new List<string>();
        private const int MaxLogLines = 300;

        public MainPage()
        {
            this.InitializeComponent();
            JsEngineSwitcher.Current.EngineFactories.Add(new ChakraCoreJsEngineFactory());
            _server.Log += OnServerLog;
            Log("ClawBox started. OpenClaw embedded v" + _embeddedOpenClawVersion + ", ChakraCore JS runtime.");
            ServerLog("(server not started yet)");
            _ = LoadEmbeddedVersionAsync();
            UpdateStatus();
        }

        private void OnServerLog(string message)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ServerLog(message));
        }

        private void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " " + message;
            _logLines.Add(line);
            if (_logLines.Count > MaxLogLines)
                _logLines.RemoveAt(0);
            LogOutput.Text = string.Join(Environment.NewLine, _logLines);
            LogScrollViewer.ChangeView(0, double.MaxValue, 1f);
        }

        private void ServerLog(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " " + message;
            _serverLogLines.Add(line);
            if (_serverLogLines.Count > MaxLogLines)
                _serverLogLines.RemoveAt(0);
            ServerLogOutput.Text = string.Join(Environment.NewLine, _serverLogLines);
            ServerLogScrollViewer.ChangeView(0, double.MaxValue, 1f);
        }

        private async Task LoadEmbeddedVersionAsync()
        {
            try
            {
                _embeddedOpenClawVersion = await EmbeddedOpenClaw.GetEmbeddedVersionAsync();
                UpdateStatus();
            }
            catch
            {
                // Keep default version
            }
        }

        private void UpdateStatus()
        {
            if (_server.IsRunning)
                StatusText.Text = "Status: Running — 127.0.0.1:" + GatewayServer.DefaultPort;
            else
                StatusText.Text = "Status: Stopped — Press Start to run server.";
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            Log("Starting embedded OpenClaw + JS runtime…");

            try
            {
                await EmbeddedOpenClaw.RunBootstrapAsync();
                _embeddedOpenClawVersion = await EmbeddedOpenClaw.GetEmbeddedVersionAsync();
                Log("Bootstrap loaded. Starting server on port " + GatewayServer.DefaultPort + "…");
                await _server.StartAsync();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                Log("Start failed: " + ex.Message);
                StatusText.Text = "Status: Stopped — Error: " + ex.Message;
            }

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            _server.Stop();
            UpdateStatus();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = true;
        }

        private static readonly Uri DashboardUri = new Uri("http://127.0.0.1:" + GatewayServer.DefaultPort + "/");

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot?.SelectedItem is PivotItem item && item.Header?.ToString() == "Dashboard")
                NavigateDashboardIfReady();
        }

        private void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateDashboardIfReady();
        }

        private void NavigateDashboardIfReady()
        {
            if (_server.IsRunning)
            {
                try
                {
                    DashboardWebView.Navigate(DashboardUri);
                    DashboardHint.Text = "Loading " + DashboardUri.ToString() + " …";
                }
                catch (Exception ex)
                {
                    DashboardHint.Text = "Error: " + ex.Message;
                }
            }
            else
            {
                DashboardWebView.NavigateToString(
                    "<!DOCTYPE html><html><head><meta charset='utf-8'><title>ClawBox</title></head><body style='font-family:Segoe UI;padding:24px;'>" +
                    "<h2>Server not running</h2><p>Go to the <b>Server</b> tab and tap <b>Start server</b>, then return here and tap <b>Refresh</b>.</p></body></html>");
                DashboardHint.Text = "Start the server on the Server tab, then tap Refresh.";
            }
        }
    }
}
