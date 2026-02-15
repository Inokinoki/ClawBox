using System;
using System.Diagnostics;
using System.IO;

namespace ClawBox.Launcher
{
    /// <summary>
    /// Full-trust launcher invoked by ClawBox UWP to start/stop the OpenClaw gateway in WSL2.
    /// Usage: ClawBox.Launcher.exe start [distro] | stop [distro]
    /// Default distro: Ubuntu
    /// </summary>
    internal static class Program
    {
        private const string DefaultDistro = "Ubuntu";
        private const int DefaultPort = 18789;

        static int Main(string[] args)
        {
            string action = args?.Length > 0 ? args[0].ToLowerInvariant() : "";
            string distro = args?.Length > 1 ? args[1] : DefaultDistro;

            if (action != "start" && action != "stop")
            {
                Console.Error.WriteLine("Usage: ClawBox.Launcher.exe start [distro] | stop [distro]");
                return 1;
            }

            try
            {
                if (action == "start")
                    return StartGateway(distro);
                return StopGateway(distro);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static int StartGateway(string distro)
        {
            // Start OpenClaw gateway in WSL; run detached so we don't block.
            var psi = new ProcessStartInfo("wsl.exe")
            {
                Arguments = $"-d {Quote(distro)} -- openclaw gateway --port {DefaultPort}",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            };
            try
            {
                Process.Start(psi);
                return 0;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.Error.WriteLine("Failed to start WSL/openclaw: " + ex.Message);
                return 1;
            }
        }

        private static int StopGateway(string distro)
        {
            var psi = new ProcessStartInfo("wsl.exe")
            {
                Arguments = $"-d {Quote(distro)} -- openclaw gateway stop",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetTempPath()
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null)
                        return 1;
                    p.WaitForExit(15000);
                    return p.HasExited ? p.ExitCode : 1;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.Error.WriteLine("Failed to run WSL/openclaw stop: " + ex.Message);
                return 1;
            }
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if (value.Contains(" ") || value.Contains("\""))
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            return value;
        }
    }
}
