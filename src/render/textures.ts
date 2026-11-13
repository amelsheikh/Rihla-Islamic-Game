// Textures made by code when the game loads: ground detail, stone walls, plaster, roofing and openings.
// Nothing here is downloaded, which keeps the game small and free of licensing questions.
import * as THREE from 'three';

function hash(x: number, y: number, seed: number) { const s = Math.sin(x * 127.1 + y * 311.7 + seed * 74.7) * 43758.5453; return s - Math.floor(s); }

/** Value noise that repeats every `cells` lattice steps, so textures tile without seams. */
function tileNoise(u: number, v: number, cells: number, seed: number) {
  const x = u * cells, y = v * cells, ix = Math.floor(x), iy = Math.floor(y), fx = x - ix, fy = y - iy;
  const a = fx * fx * (3 - 2 * fx), b = fy * fy * (3 - 2 * fy), w = (n: number) => ((n % cells) + cells) % cells;
  return (hash(w(ix), w(iy), seed) * (1 - a) + hash(w(ix + 1), w(iy), seed) * a) * (1 - b) + (hash(w(ix), w(iy + 1), seed) * (1 - a) + hash(w(ix + 1), w(iy + 1), seed) * a) * b;
}
export function tileFbm(u: number, v: number, cells: number, octaves: number, seed: number) {
  let s = 0, amp = 0.5, norm = 0;
  for (let o = 0; o < octaves; o++) { s += tileNoise(u, v, cells << o, seed + o) * amp; norm += amp; amp *= 0.5; }
  return s / norm;
}

/** Height field (0..1) to an RGBA texture: normal in RGB, the height itself in A. */
function heightTexture(h: Float32Array, size: number, strength: number, repeat = true) {
  const d = new Uint8Array(size * size * 4), at = (x: number, y: number) => h[((y + size) % size) * size + ((x + size) % size)];
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const dx = (at(x + 1, y) - at(x - 1, y)) * strength, dy = (at(x, y + 1) - at(x, y - 1)) * strength, l = 1 / Math.hypot(dx, dy, 1), i = (y * size + x) * 4;
    d[i] = (-dx * l * 0.5 + 0.5) * 255; d[i + 1] = (-dy * l * 0.5 + 0.5) * 255; d[i + 2] = (l * 0.5 + 0.5) * 255; d[i + 3] = Math.min(255, Math.max(0, h[y * size + x] * 255));
  }
  const t = new THREE.DataTexture(d, size, size, THREE.RGBAFormat);
  if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.minFilter = THREE.LinearMipmapLinearFilter; t.magFilter = THREE.LinearFilter; t.generateMipmaps = true; t.anisotropy = 4; t.needsUpdate = true;
  return t;
}

/** Two tiling ground textures: broken rock for slopes and stony soil for flatter ground. */
export function groundDetail() {
  const S = 256, rock = new Float32Array(S * S), soil = new Float32Array(S * S);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const u = x / S, v = y / S;
    const ridged = 1 - Math.abs(tileFbm(u, v, 5, 5, 3) * 2 - 1);
    rock[y * S + x] = Math.min(1, ridged * 0.75 + tileFbm(u, v, 24, 3, 9) * 0.35);
    const pebbles = Math.pow(tileNoise(u, v, 40, 21), 3) * 0.9;
    soil[y * S + x] = Math.min(1, tileFbm(u, v, 8, 5, 5) * 0.7 + pebbles * 0.5 + hash(x, y, 2) * 0.12);
  }
  return { rock: heightTexture(rock, S, 3.2), soil: heightTexture(soil, S, 2.2) };
}

export function waterNormals() {
  const S = 256, h = new Float32Array(S * S);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) h[y * S + x] = tileFbm(x / S, y / S, 6, 4, 31);
  return heightTexture(h, S, 2.0);
}

function canvasTexture(cv: HTMLCanvasElement, srgb: boolean, repeat = true) {
  const t = new THREE.CanvasTexture(cv);
  t.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
  if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.anisotropy = 8;
  return t;
}
function normalFromCanvas(cv: HTMLCanvasElement, strength: number) {
  const S = cv.width, px = cv.getContext('2d')!.getImageData(0, 0, S, S).data, h = new Float32Array(S * S);
  for (let i = 0; i < S * S; i++) h[i] = px[i * 4] / 255;
  return heightTexture(h, S, strength);
}

/** Coursed limestone blocks, the building stone of Al-Quds. One tile covers about 4 m. */
export function stoneWall() {
  const S = 512, col = document.createElement('canvas'), hgt = document.createElement('canvas');
  col.width = col.height = hgt.width = hgt.height = S;
  const c = col.getContext('2d')!, g = hgt.getContext('2d')!;
  c.fillStyle = '#8f8468'; c.fillRect(0, 0, S, S); g.fillStyle = '#000'; g.fillRect(0, 0, S, S);
  let seed = 5; const r = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);
  const rows = 9, rh = S / rows;
  for (let j = 0; j < rows; j++) {
    let x = -r() * 60;
    while (x < S) {
      const w = 46 + r() * 70, v = 0.84 + r() * 0.26, warm = r() * 14;
      for (const off of [0, S]) {
        c.fillStyle = `rgb(${(206 + warm) * v},${196 * v},${(160 - warm) * v})`; c.fillRect(x + off - S + 2, j * rh + 2, w - 3, rh - 3);
        c.fillRect(x + off + 2 - (off ? S : 0), j * rh + 2, w - 3, rh - 3);
        const hv = 150 + r() * 100; g.fillStyle = `rgb(${hv},${hv},${hv})`;
        g.fillRect(x + off - S + 3, j * rh + 3, w - 5, rh - 5); g.fillRect(x + off + 3 - (off ? S : 0), j * rh + 3, w - 5, rh - 5);
      }
      x += w;
    }
  }
  // Weathering: speckle and soft streaks.
  const id = c.getImageData(0, 0, S, S), d = id.data;
  for (let y = 0; y < S; y++) for (let x2 = 0; x2 < S; x2++) {
    const k = 0.84 + tileFbm(x2 / S, y / S, 6, 4, 11) * 0.3 + (hash(x2, y, 4) - 0.5) * 0.08, i = (y * S + x2) * 4;
    d[i] *= k; d[i + 1] *= k; d[i + 2] *= k;
  }
  c.putImageData(id, 0, 0);
  return { map: canvasTexture(col, true), normalMap: normalFromCanvas(hgt, 2.4) };
}

/** Lime-washed plaster for village houses and domes. */
export function plaster() {
  const S = 256, cv = document.createElement('canvas'); cv.width = cv.height = S;
  const c = cv.getContext('2d')!, id = c.createImageData(S, S);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const k = 0.86 + tileFbm(x / S, y / S, 5, 5, 17) * 0.2 + (hash(x, y, 8) - 0.5) * 0.05, i = (y * S + x) * 4;
    id.data[i] = 236 * k; id.data[i + 1] = 228 * k; id.data[i + 2] = 208 * k; id.data[i + 3] = 255;
  }
  c.putImageData(id, 0, 0);
  return { map: canvasTexture(cv, true) };
}

/** Lead roofing in sheets with raised seams, as on the great domes in 1910. */
export function leadSheet() {
  const S = 256, cv = document.createElement('canvas'); cv.width = cv.height = S;
  const c = cv.getContext('2d')!;
  c.fillStyle = '#6a7078'; c.fillRect(0, 0, S, S);
  for (let i = 0; i < 8; i++) { c.fillStyle = i % 2 ? '#5b6168' : '#747a82'; c.fillRect(i * 32 + 3, 0, 26, S); c.fillStyle = '#3d4248'; c.fillRect(i * 32, 0, 3, S); }
  const id = c.getImageData(0, 0, S, S);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) { const k = 0.85 + tileFbm(x / S, y / S, 7, 4, 23) * 0.3, i = (y * S + x) * 4; id.data[i] *= k; id.data[i + 1] *= k; id.data[i + 2] *= k; }
  c.putImageData(id, 0, 0);
  return { map: canvasTexture(cv, true) };
}

/** One face of the Dome of the Rock's octagon: marble panels below, Ottoman tilework and arched bays above. */
export function octagonFace() {
  const W = 512, H = 320, cv = document.createElement('canvas'); cv.width = W; cv.height = H;
  const c = cv.getContext('2d')!;
  let seed = 9; const r = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);
  // Marble dado.
  for (let i = 0; i < 14; i++) { const v = 200 + r() * 40; c.fillStyle = `rgb(${v},${v - 4},${v - 14})`; c.fillRect((i * W) / 14, H * 0.5, W / 14 - 2, H * 0.5); }
  c.strokeStyle = 'rgba(120,110,100,0.35)';
  for (let i = 0; i < 60; i++) { c.beginPath(); const x = r() * W, y = H * 0.5 + r() * H * 0.5; c.moveTo(x, y); c.lineTo(x + (r() - 0.5) * 40, y + r() * 30); c.stroke(); }
  // Tile field: blues and turquoise with small repeating motifs. The inscription band is left as a plain
  // dark blue frieze; real calligraphy must be added by an artist and reviewed (see NEEDS_REVIEW.md).
  c.fillStyle = '#2f6f9a'; c.fillRect(0, 0, W, H * 0.5);
  for (let y = 26; y < H * 0.5; y += 9) for (let x = 0; x < W; x += 9) {
    c.fillStyle = ['#1f5a8a', '#3d8fb0', '#e8dfc0', '#2a7f7a', '#d9b24a'][Math.floor(r() * 5)];
    c.fillRect(x + 2, y + 2, 5, 5);
  }
  c.fillStyle = '#17385e'; c.fillRect(0, 0, W, 24);
  c.fillStyle = '#d8c98a'; c.fillRect(0, 22, W, 3); c.fillRect(0, H * 0.5 - 3, W, 4);
  // Seven arched bays.
  for (let i = 0; i < 7; i++) {
    const cx = ((i + 0.5) * W) / 7, w = 44, top = 44, bottom = H * 0.5 - 10;
    c.fillStyle = '#e9e2cc'; c.beginPath(); c.moveTo(cx - w / 2 - 5, bottom); c.lineTo(cx - w / 2 - 5, top + 26); c.quadraticCurveTo(cx, top - 26, cx + w / 2 + 5, top + 26); c.lineTo(cx + w / 2 + 5, bottom); c.fill();
    c.fillStyle = i === 0 || i === 6 ? '#3f7fa0' : '#1c2b3a';
    c.beginPath(); c.moveTo(cx - w / 2, bottom); c.lineTo(cx - w / 2, top + 28); c.quadraticCurveTo(cx, top - 16, cx + w / 2, top + 28); c.lineTo(cx + w / 2, bottom); c.fill();
  }
  return { map: canvasTexture(cv, true, false) };
}

/** Doors and windows, four to a sheet: arched door, arched window, shuttered window, great gate. */
export function openings() {
  const S = 512, cv = document.createElement('canvas'); cv.width = cv.height = S;
  const c = cv.getContext('2d')!, q = S / 2;
  const arch = (ox: number, oy: number, fill: string, inset: number) => {
    c.fillStyle = fill; c.beginPath();
    c.moveTo(ox + inset, oy + q - 6); c.lineTo(ox + inset, oy + q * 0.45); c.quadraticCurveTo(ox + q / 2, oy - q * 0.12 + inset, ox + q - inset, oy + q * 0.45); c.lineTo(ox + q - inset, oy + q - 6); c.fill();
  };
  // Arched wooden door.
  arch(0, 0, '#b9ad8c', 26); arch(0, 0, '#3b2a1c', 44);
  c.strokeStyle = '#20160d'; c.lineWidth = 3; for (let x = 60; x < q - 50; x += 22) { c.beginPath(); c.moveTo(x, 70); c.lineTo(x, q - 8); c.stroke(); }
  // Arched window with a dark interior and a grille.
  arch(q, 0, '#b9ad8c', 60); arch(q, 0, '#16202a', 76);
  c.strokeStyle = '#5a4a36'; c.lineWidth = 3; for (let x = q + 90; x < S - 80; x += 20) { c.beginPath(); c.moveTo(x, 60); c.lineTo(x, q - 10); c.stroke(); }
  // Rectangular window with green shutters.
  c.fillStyle = '#b9ad8c'; c.fillRect(62, q + 50, q - 124, q - 100); c.fillStyle = '#16202a'; c.fillRect(76, q + 64, q - 152, q - 128);
  c.fillStyle = '#3f6b55'; c.fillRect(76, q + 64, 36, q - 128); c.fillRect(q - 112, q + 64, 36, q - 128);
  // Great gate: deep pointed arch with a studded door.
  arch(q, q, '#a89c7c', 8); arch(q, q, '#2c2118', 34);
  c.fillStyle = '#6b5a3a'; for (let y = q + 90; y < S - 20; y += 26) for (let x = q + 56; x < S - 50; x += 26) c.fillRect(x, y, 5, 5);
  const t = canvasTexture(cv, true, false);
  return { map: t };
}

/** The packed-stone road: paler wheel tracks, darker verges, and a matching relief. */
export function roadSurface() {
  const W = 256, H = 512, col = document.createElement('canvas'), hc = document.createElement('canvas');
  col.width = hc.width = W; col.height = hc.height = H;
  const c = col.getContext('2d')!, id = c.createImageData(W, H), g = hc.getContext('2d')!, hid = g.createImageData(W, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const u = x / W, v = y / H, edge = Math.min(1, Math.min(u, 1 - u) / 0.1);
    const track = Math.max(0, 1 - Math.abs(Math.abs(u - 0.5) - 0.14) / 0.07);
    const n = tileFbm(u, v * 2, 8, 5, 41), stones = Math.pow(tileNoise(u, v * 2, 48, 43), 2);
    const k = 0.6 + edge * 0.12 + track * 0.1 + n * 0.16 + stones * 0.06, i = (y * W + x) * 4;
    id.data[i] = 186 * k; id.data[i + 1] = 168 * k; id.data[i + 2] = 134 * k; id.data[i + 3] = 255;
    const hv = (n * 0.5 + stones * 0.5 - track * 0.25) * 255; hid.data[i] = hid.data[i + 1] = hid.data[i + 2] = hv; hid.data[i + 3] = 255;
  }
  c.putImageData(id, 0, 0); g.putImageData(hid, 0, 0);
  const px = g.getImageData(0, 0, W, H).data, S = 256, h = new Float32Array(S * S);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) h[y * S + x] = px[(y * 2 * W + x) * 4] / 255;
  return { map: canvasTexture(col, true), normalMap: heightTexture(h, S, 0.6) };
}
