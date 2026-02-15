// Embedded OpenClaw bootstrap — runs in ChakraCore JS runtime.
// Defines gateway identity for ClawBox (full gateway runs under Node/Bun elsewhere).
var openclaw = {
  embedded: true,
  version: '1.0.0',
  gateway: {
    port: 18789,
    name: 'ClawBox'
  }
};
