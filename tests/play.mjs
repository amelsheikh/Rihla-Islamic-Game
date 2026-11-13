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
await page.goto(url);
await page.waitForFunction(() => window.__rihla?.ready, null, { timeout: 120000 });
await page.screenshot({ path: `${out}/menu.png` });
await page.click('#start');
await page.evaluate(() => window.__rihla.quality('low'));
await page.keyboard.down('ArrowUp');
await page.waitForFunction(() => window.__rihla.stats().speed > 3, null, { timeout: 120000 });
await page.keyboard.up('ArrowUp');
check(true, 'the car pulls away under keyboard control');
check((await page.textContent('#alt')).includes('above sea level'), 'HUD shows true elevation: ' + (await page.textContent('#alt')));
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
