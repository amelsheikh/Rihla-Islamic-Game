// Opens the game in a headless browser and saves screenshots at several places and times of day.
// Usage: node tests/screenshots.mjs [outDir] [baseUrl] [shot,shot,...]
import fs from 'node:fs';
import { createRequire } from 'node:module';

// Playwright is installed globally in the cloud environment; locally, `npm i -D playwright` also works.
const { chromium } = createRequire(import.meta.url)('playwright');

const out = process.argv[2] || 'test-results/shots';
const base = process.argv[3] || 'http://localhost:5173/';
const only = process.argv[4]?.split(',');
fs.mkdirSync(out, { recursive: true });

// name, distance along the road (m), hour, facing back up the road?, camera
const SHOTS = [
  ['quds-panorama', { place: 'mount-of-olives', dx: -40, dy: 14, yaw: 1.5708, pitch: -0.1 }, 8, false, 'free'],
  ['quds-morning', 60, 8.5, true, 'chase'],
  ['car-side', { car: 3300, dx: 5.5, dy: 1.3, dz: -3.5, yaw: 2.14, pitch: -0.12 }, 16, false, 'free'],
  ['car-rear', { car: 5200, dx: -4.2, dy: 1.5, dz: 4.6, yaw: -0.74, pitch: -0.14 }, 10, false, 'free'],
  ['quds-sunset', 330, 17.7, true, 'chase'],
  ['wilderness-noon', 3300, 12, false, 'chase'],
  ['wilderness-sunrise', 4300, 6.3, false, 'chase'],
  ['sea-level-afternoon', -1, 15.5, false, 'chase'],
  ['ariha-sunset', 7350, 17.8, false, 'chase'],
  ['deadsea-night', 9300, 22, false, 'chase'],
  ['bonnet-morning', 2000, 9, false, 'bonnet'],
];

const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 });
const errors = [];
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', (e) => errors.push(String(e)));
await page.goto(base + '?autostart=1', { waitUntil: 'load' });
await page.waitForFunction(() => window.__rihla?.ready, null, { timeout: 120000 });
await page.evaluate(() => { window.__rihla.quality('high'); });
page.setDefaultTimeout(240000);
for (const [name, s, hour, reverse, cam] of SHOTS) {
  if (only && !only.includes(name)) continue;
  await page.evaluate(([s, hour, reverse, cam]) => {
    const r = window.__rihla;
    r.setTime(hour, 0); r.camera(cam);
    if (typeof s === 'object' && s.car) { r.teleport(s.car, false); const p = r.car.body.translation(); r.freeCam(p.x + s.dx, p.y + s.dy, p.z + s.dz, s.yaw, s.pitch); }
    else if (typeof s === 'object') { const p = r.world.data.places.find((q) => q.id === s.place); r.teleport(200, false); r.freeCam(p.x + s.dx, p.y + s.dy, p.z, s.yaw, s.pitch); }
    else r.teleport(s < 0 ? r.world.data.road.seaLevelAt - 14 : s, reverse);
    r.rebake();
  }, [s, hour, reverse, cam]);
  const f0 = await page.evaluate(() => window.__rihla.stats().frame);
  await page.waitForFunction((f) => window.__rihla.stats().frame > f + 14, f0, { timeout: 120000 });
  await page.screenshot({ path: `${out}/${name}.png` });
  console.log(name, JSON.stringify(await page.evaluate(() => { const s = window.__rihla.stats(); return { calls: s.calls, tris: s.triangles, geo: s.geometries, tex: s.textures }; })));
}
if (errors.length) console.log('ERRORS:\n' + [...new Set(errors)].slice(0, 12).join('\n'));
await browser.close();
process.exit(errors.length ? 1 : 0);
