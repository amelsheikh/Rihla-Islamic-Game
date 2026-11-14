// Screenshots on foot: by the car at the gate, and on the Haram platform facing the Dome of the Rock.
// Usage: node tests/walk-shots.mjs <outDir> [baseUrl]
import fs from 'node:fs';
import { createRequire } from 'node:module';
const { chromium } = createRequire(import.meta.url)('playwright');
const out = process.argv[2] || 'test-results/walk', base = process.argv[3] || 'http://localhost:5173/';
fs.mkdirSync(out, { recursive: true });
const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
page.setDefaultTimeout(300000);
await page.goto(base + '?autostart=1');
await page.waitForFunction(() => window.__rihla?.ready);
await page.evaluate(() => { const r = window.__rihla; r.quality('high'); r.setTime(9, 0); r.teleport(6); r.rebake(); });
const frames = async (n) => { const f0 = await page.evaluate(() => window.__rihla.stats().frame); await page.waitForFunction((f) => window.__rihla.stats().frame > f, f0 + n); };
await frames(6);
await page.evaluate(() => window.__rihla.toggleFoot());
await frames(14);
await page.screenshot({ path: `${out}/walk-gate.png` });
const pos = await page.evaluate(() => {
  const r = window.__rihla, d = r.world.data.places.find((p) => p.id === 'dome-of-the-rock');
  r.walker.place(d.x + 30, d.z + 6, Math.PI / 2 - 0.2); // east of the Dome, facing west towards it
  return { x: r.walker.pos.x, y: r.walker.pos.y, z: r.walker.pos.z };
});
await frames(16);
await page.screenshot({ path: `${out}/walk-dome.png` });
console.log('walker on the platform at', JSON.stringify(pos));
await browser.close();
