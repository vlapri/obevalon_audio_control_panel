// SPDX-License-Identifier: GPL-3.0-or-later
// Renders the SVG icon sources into multi-size .ico files (PNG-compressed frames).
//
// Requirements: Node.js with Playwright and Chromium.
// Usage:  node build/render-icons.mjs
// Output: src/Obevalon.AudioControlPanel/Assets/{app,tray,tray-muted,tray-update}.ico
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { chromium } = require('playwright');

const assets = join(dirname(fileURLToPath(import.meta.url)), '..', 'src', 'Obevalon.AudioControlPanel', 'Assets');
const previewDir = process.env.ICON_PREVIEW_DIR; // optional: also write the PNG frames here

// Small sizes use the simplified tray design so they stay legible.
const icons = {
  'app.ico': [...[16, 20, 24, 32].map(s => ['tray.svg', s]), ...[40, 48, 64, 96, 128, 256].map(s => ['app.svg', s])],
  'tray.ico': [16, 20, 24, 32, 40, 48, 64].map(s => ['tray.svg', s]),
  'tray-muted.ico': [16, 20, 24, 32, 40, 48, 64].map(s => ['tray-muted.svg', s]),
  'tray-update.ico': [16, 20, 24, 32, 40, 48, 64].map(s => ['tray-update.svg', s]),
};

async function render(page, svgName, size) {
  const svg = readFileSync(join(assets, svgName), 'utf8');
  await page.setViewportSize({ width: size, height: size });
  await page.setContent(
    `<html><body style="margin:0;background:transparent"><div style="width:${size}px;height:${size}px">${svg}</div>` +
    '<style>svg{width:100%;height:100%;display:block}</style></body></html>');
  return page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
}

// ICO container with PNG frames (supported since Windows Vista).
function buildIco(frames) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0); // reserved
  header.writeUInt16LE(1, 2); // type: icon
  header.writeUInt16LE(frames.length, 4);

  const entries = [];
  let offset = 6 + 16 * frames.length;
  for (const { size, png } of frames) {
    const entry = Buffer.alloc(16);
    entry.writeUInt8(size >= 256 ? 0 : size, 0); // width (0 = 256)
    entry.writeUInt8(size >= 256 ? 0 : size, 1); // height
    entry.writeUInt8(0, 2);                      // palette
    entry.writeUInt8(0, 3);                      // reserved
    entry.writeUInt16LE(1, 4);                   // color planes
    entry.writeUInt16LE(32, 6);                  // bits per pixel
    entry.writeUInt32LE(png.length, 8);
    entry.writeUInt32LE(offset, 12);
    entries.push(entry);
    offset += png.length;
  }
  return Buffer.concat([header, ...entries, ...frames.map(f => f.png)]);
}

const browser = await chromium.launch();
const page = await browser.newPage({ deviceScaleFactor: 1 });
if (previewDir) mkdirSync(previewDir, { recursive: true });
for (const [icoName, list] of Object.entries(icons)) {
  const frames = [];
  for (const [svgName, size] of list) {
    const png = await render(page, svgName, size);
    frames.push({ size, png });
    if (previewDir) writeFileSync(join(previewDir, `${icoName.replace('.ico', '')}-${size}.png`), png);
  }
  writeFileSync(join(assets, icoName), buildIco(frames));
  console.log(`${icoName}: ${list.map(([, s]) => s).join(', ')}`);
}
await browser.close();
