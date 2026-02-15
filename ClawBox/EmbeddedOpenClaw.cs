using System;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using Windows.ApplicationModel;
using Windows.Storage;

namespace ClawBox
{
    /// <summary>
    /// Loads and runs the embedded OpenClaw bootstrap script in the ChakraCore JS runtime.
    /// </summary>
    public static class EmbeddedOpenClaw
    {
        public const string BootstrapPath = "Assets/OpenClaw/bootstrap.js";
        public const string VersionPath = "Assets/OpenClaw/version.txt";

        /// <summary>
        /// Loads the embedded bootstrap.js from the app package and runs it in the default JS engine.
        /// Returns the openclaw.version string from the script, or a fallback if load/run fails.
        /// </summary>
        public static async Task<string> GetEmbeddedVersionAsync()
        {
            try
            {
                var folder = Package.Current.InstalledLocation;
                var file = await folder.GetFileAsync(BootstrapPath);
                string script = await FileIO.ReadTextAsync(file);

                using (var engine = JsEngineSwitcher.Current.CreateDefaultEngine())
                {
                    engine.Execute(script);
                    string version = engine.Evaluate("openclaw && openclaw.version ? openclaw.version : '?'").ToString();
                    return string.IsNullOrEmpty(version) ? "1.0.0" : version;
                }
            }
            catch
            {
                try
                {
                    var folder = Package.Current.InstalledLocation;
                    var vFile = await folder.GetFileAsync(VersionPath);
                    return await FileIO.ReadTextAsync(vFile) ?? "1.0.0";
                }
                catch
                {
                    return "1.0.0";
                }
            }
        }

        /// <summary>
        /// Runs the embedded bootstrap script (no return). Use to ensure OpenClaw + JS runtime are loaded.
        /// </summary>
        public static async Task RunBootstrapAsync()
        {
            try
            {
                var folder = Package.Current.InstalledLocation;
                var file = await folder.GetFileAsync(BootstrapPath);
                string script = await FileIO.ReadTextAsync(file);
                using (var engine = JsEngineSwitcher.Current.CreateDefaultEngine())
                    engine.Execute(script);
            }
            catch
            {
                // Ignore; version fallback will be used
            }
        }
    }
}
