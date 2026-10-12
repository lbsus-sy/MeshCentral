#!/usr/bin/env node
'use strict';

// Experimental launcher for Wispbyte's Node.js app runtime.
// The provider must expose a stable public hostname, WebSocket routing, and
// persistent storage. See README.md before using with real devices.
const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');

const root = path.resolve(__dirname, '../..');
const dataDir = path.join(root, 'meshcentral-data');
const configPath = path.join(dataDir, 'config.json');
const publicPort = Number(process.env.PORT || process.env.SERVER_PORT || process.env.APP_PORT);

function normalizeHostname(value) {
    if (!value || typeof value !== 'string') return '';
    let host = value.trim();
    host = host.replace(/^https?:\/\//i, '').split('/')[0].split(':')[0].replace(/\.$/, '');
    if (!host || /[\s/]/.test(host) || host === 'localhost' || !host.includes('.')) return '';
    return host.toLowerCase();
}

const hostCandidates = [
    process.env.MESH_HOSTNAME,
    process.env.PUBLIC_URL,
    process.env.APP_URL,
    process.env.RENDER_EXTERNAL_URL,
    process.env.RAILWAY_PUBLIC_DOMAIN,
    process.env.FLY_PUBLIC_IP ? process.env.FLY_APP_NAME : '',
    process.env.HOSTNAME
];
const hostname = hostCandidates.map(normalizeHostname).find(Boolean) || '';

if (!Number.isInteger(publicPort) || publicPort < 1 || publicPort > 65535) {
    console.error('No valid runtime port was detected. Set PORT to the internal port assigned by the Node.js host.');
    process.exit(1);
}
if (!hostname) {
    console.error('No public hostname was detected. Set MESH_HOSTNAME to the assigned public hostname (domain only; no scheme or port).');
    process.exit(1);
}

fs.mkdirSync(dataDir, { recursive: true });
const config = fs.existsSync(configPath)
    ? JSON.parse(fs.readFileSync(configPath, 'utf8'))
    : { settings: {}, domains: { '': {} } };
config.settings ||= {};
config.domains ||= {};
config.domains[''] ||= {};

// Apply only deployment-critical settings. Preserve the rest of the existing
// configuration so restarting the app does not reset administrator settings.
Object.assign(config.settings, {
    cert: hostname,
    port: publicPort,
    redirPort: 0,
    tlsOffload: true,
    WANonly: true
});
Object.assign(config.domains[''], {
    title: 'Lakers MeshCentral',
    title2: 'Purple & Gold Remote Management',
    welcomeText: 'Authorized device management. Only enroll devices you own or are explicitly authorized to manage.',
    NewAccounts: false,
    siteStyle: 3,
    agentCustomization: {
        displayName: 'Lakers Mesh Agent',
        description: 'Authorized device management service',
        companyName: 'Lakers MeshCentral',
        serviceName: 'lakersmeshagent',
        installText: 'Install this agent only if you trust and are authorized to connect to the displayed MeshCentral server.',
        foregroundColor: '#FDB927',
        backgroundColor: '#552583'
    }
});
fs.writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n', { mode: 0o600 });

const cssSource = path.join(__dirname, '../branding/web/public/styles/custom.css');
const cssTarget = path.join(root, 'meshcentral-web/public/styles/custom.css');
fs.mkdirSync(path.dirname(cssTarget), { recursive: true });
fs.copyFileSync(cssSource, cssTarget);

console.log('Starting MeshCentral with the Lakers theme.');
console.log('The runtime port and detected public hostname are configured; values are not printed.');
console.log('Verify persistent storage and agent/WebSocket connectivity before enrolling real devices.');

const child = spawn(process.execPath, [
    path.join(root, 'meshcentral.js'),
    '--configfile',
    configPath
], { cwd: root, stdio: 'inherit', env: process.env });

let stopping = false;
function shutdown(signal) {
    if (stopping) return;
    stopping = true;
    if (child.exitCode === null) child.kill(signal);
}
process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));
child.on('error', (err) => {
    console.error('Unable to start MeshCentral:', err.message);
    process.exitCode = 1;
});
child.on('exit', (code, signal) => {
    process.exitCode = typeof code === 'number' ? code : (signal ? 1 : 0);
});
