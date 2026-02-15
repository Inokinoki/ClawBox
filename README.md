# ClawBox

UWP app that runs an OpenClaw-compatible server on **Xbox** and Windows, with start/stop control. **OpenClaw** and the **ChakraCore** JavaScript runtime are **embedded** in the app (no WSL, no desktop-only features).

## Features

- **Xbox and Windows**: Targets both `Windows.Universal` and `Windows.Xbox` device families.
- **Embedded OpenClaw**: Bootstrap script and version are in `Assets/OpenClaw/` (bootstrap.js, version.txt). The bootstrap runs in the embedded JS runtime at startup and on Start.
- **Embedded JS runtime**: **ChakraCore** is embedded via JavaScriptEngineSwitcher.ChakraCore; the embedded OpenClaw bootstrap runs inside it.
- **Start / Stop server**: In-process server on port **18789** (OpenClaw default), so you can start and shut it down from the app.

## Running the full OpenClaw gateway on Xbox

The **OpenClaw gateway** is a full **Node.js** (or Bun) app with `require()`, npm modules, and network APIs. It cannot run inside ChakraCore alone.

- **This app**: Embeds **OpenClaw** (bootstrap script in `Assets/OpenClaw/`) and **ChakraCore** JS runtime; runs the bootstrap in ChakraCore and a server on port 18789. Start/Stop control the in-process server and re-run the embedded bootstrap.
- **Full gateway on device**: To run the actual OpenClaw gateway **on** Xbox you need a **Node.js UWP** runtime, for example:
  - [node-uwp-wrapper](https://github.com/ms-iot/node-uwp-wrapper) (archived): Wraps a Chakra-based Node build as a UWP background app (`node.dll`). You would build Node from the chakra-uwp branch, package it with your app, and run the OpenClaw gateway script from that runtime.
- **Bun**: There is no Bun runtime for UWP/Xbox; Bun is desktop-only.

So: ClawBox gives you **Xbox + start/stop + ChakraCore** today; for the **full OpenClaw gateway on Xbox** you’d integrate a Node UWP runtime (e.g. node-uwp-wrapper) and run the gateway script there.

## Build and run

1. Open `ClawBox.sln` in Visual Studio.
2. Set configuration (e.g. Debug) and platform (x86, x64, or ARM64 for Xbox).
3. Build and run (F5). On Xbox, deploy to your device or use the Xbox One Developer Mode app.

## Capabilities

- `internetClient`: General network access.
- `privateNetworkClientServer`: So the in-process server can listen on port 18789.

## Port

Server listens on **127.0.0.1:18789** (same port as OpenClaw gateway). Change `GatewayServer.DefaultPort` in code if you need another port.

## Cannot access http://localhost:18789 from browser?

UWP apps run in an **AppContainer** (sandbox). Windows applies **network isolation** to that container, so by default **other processes (e.g. your browser) may not be able to connect** to a port the UWP app is listening on, even on localhost. That can be why http://localhost:18789/ fails or times out.

**Fix: add a loopback exemption** so the app can receive loopback connections:

1. **Get the app’s Package Family Name**  
   After installing or running the app once, in PowerShell:
   ```powershell
   Get-AppxPackage -Name *ClawBox* | Select-Object -ExpandProperty PackageFamilyName
   ```
   Example output: `ClawBox_xxxxxxxxxxxx` (your PublisherId will differ).

2. **Add loopback exemption** (run PowerShell **as Administrator**):
   ```powershell
   CheckNetIsolation LoopbackExempt -a -n=<PackageFamilyName>
   ```
   Replace `<PackageFamilyName>` with the value from step 1, e.g.:
   ```powershell
   CheckNetIsolation LoopbackExempt -a -n=ClawBox_7f0f196b65c84c60
   ```
   (Use the exact string from `Get-AppxPackage`; the name may not include the full GUID.)

3. **Try again**  
   Start the server in ClawBox, then open http://localhost:18789/ in your browser.

**To remove the exemption later:**
```powershell
CheckNetIsolation LoopbackExempt -d -n=<PackageFamilyName>
```

When you run from Visual Studio (F5), the debugger sometimes adds a temporary exemption; if you run the installed app without VS, you usually need the step above.
