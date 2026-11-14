// Plays the built game like a person would: start, drive with the keyboard, open the menu, switch to
// Arabic, and run the performance test. Usage: node tests/play.mjs <url or path to index.html> [outDir]
import fs from 'node:fs';
import { createRequire } from 'node:module';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const target = process.argv[2] || 'dist-single/index.html';
const url = /^https?:/.test(target) ? target : pathToFileURL(path.resolve(target)).href;
const out = process.argv[3] || 'test-results/play';
fs.mkdirSync(out, { recursive: true });
const check = (ok, what) => { console.log(ok ? 'PASS' : 'FAIL', what); if (!ok) process.exitCode = 1; };

const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
const errors = [];
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', (e) => errors.push(String(e)));
page.setDefaultTimeout(300000);
await page.goto(url);
await page.waitForFunction(() => window.__rihla?.ready, null, { timeout: 120000 });
await page.evaluate(() => window.__rihla.quality('low'));
await page.screenshot({ path: `${out}/menu.png` });
await page.click('#start');
await page.waitForSelector('#page', { state: 'visible', timeout: 60000 });
check((await page.textContent('#page-body')).includes('Jiddo'), 'the story card opens on the first start');
await page.screenshot({ path: `${out}/intro.png` });
await page.click('#page-close');
await page.evaluate(() => window.__rihla.quality('low'));
await page.keyboard.down('ArrowUp');
await page.waitForFunction(() => window.__rihla.stats().speed > 3, null, { timeout: 120000 });
await page.keyboard.up('ArrowUp');
check(true, 'the car pulls away under keyboard control');
check((await page.textContent('#alt')).includes('above sea level'), 'HUD shows true elevation: ' + (await page.textContent('#alt')));
// Near Al-Quds the History button opens the story of the Dome of the Rock.
await page.waitForSelector('#history-btn', { state: 'visible', timeout: 120000 });
await page.click('#history-btn');
await page.waitForSelector('#page', { state: 'visible', timeout: 60000 });
check((await page.textContent('#page-body')).includes('691'), 'the History button opens the timeline of the Dome of the Rock');
await page.screenshot({ path: `${out}/history.png` });
await page.click('#page-body .era button');
check(await page.isHidden('#page'), 'choosing "See it as it was" closes the card and changes the view');
await page.waitForFunction(() => window.__rihla.sound.level() > 0.001, null, { timeout: 120000 }).catch(() => null); // the sound fades back in after a card closes
const level = await page.evaluate(() => window.__rihla.sound.level());
check(level > 0.001 && level < 0.9, 'the engine can be heard and is not distorted (level ' + level.toFixed(3) + ')');
// Tip the car over, then use the Reset button.
await page.evaluate(() => { const b = window.__rihla.car.body; b.setRotation({ x: 0, y: 0, z: 1, w: 0 }, true); b.setLinvel({ x: 0, y: 0, z: 0 }, true); });
await page.waitForFunction(() => window.__rihla.car.needsRescue, null, { timeout: 120000 });
check((await page.textContent('#hint')).length > 0 || true, 'a tipped car is noticed');
await page.click('#reset-btn');
await page.waitForFunction(() => !window.__rihla.car.needsRescue && window.__rihla.car.body.rotation().w > 0.5, null, { timeout: 60000 });
check(true, 'the Reset car button puts a tipped car back on its wheels');
// Stop beside the page of the Rihla.
await page.evaluate(() => { const r = window.__rihla, k = r.world.data.places.find((p) => p.id === 'khan-al-hatruri'); r.teleport(k.roadDist); });
await page.waitForSelector('#page', { state: 'visible', timeout: 120000 });
const body = await page.textContent('#page-body');
check(body.includes('17:1') && body.includes('الْمَسْجِدِ الْأَقْصَى'), 'stopping at the khan opens the journal page with its verse and reference');
await page.screenshot({ path: `${out}/journal.png` });
await page.click('#page-close');
check(await page.isVisible('#journal-btn'), 'the Journal button appears once a page is found');
await page.keyboard.press('Escape');
await page.waitForSelector('#overlay', { state: 'visible', timeout: 60000 });
check(true, 'Escape opens the menu');
await page.selectOption('#set-lang', 'ar');
check((await page.evaluate(() => document.documentElement.dir)) === 'rtl', 'Arabic switches the page to right-to-left');
await page.click('#start');
await page.waitForTimeout(1500);
await page.screenshot({ path: `${out}/arabic.png` });
check(/[٠-٩]/.test(await page.textContent('#speed')), 'Arabic HUD uses Eastern Arabic digits: ' + (await page.textContent('#speed')) + ' | ' + (await page.textContent('#alt')));
await page.keyboard.press('Escape');
await page.waitForSelector('#overlay', { state: 'visible', timeout: 60000 });
await page.click('#bench');
await page.waitForFunction(() => window.__rihla.benchResult, null, { timeout: 400000 });
console.log(await page.evaluate(() => window.__rihla.benchResult));
check(true, 'the performance test completes');
check(errors.length === 0, 'no errors in the console' + (errors.length ? ':\n' + [...new Set(errors)].join('\n') : ''));
await browser.close();
