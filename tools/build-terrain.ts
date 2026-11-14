// Offline pipeline: real elevation data -> warped game terrain + a drivable road through the waypoints.
// Run with `npm run terrain`. Output goes to src/generated/. See DESIGN.md section 3.
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import sharp from 'sharp';
import { toGame, toReal, toGameY, V_SCALE } from '../src/world/warp';

const ROOT = path.resolve(import.meta.dirname, '..');
const CACHE = path.join(ROOT, 'tools/.cache');
const OUT = path.join(ROOT, 'src/generated');
const PREVIEW = process.env.PREVIEW_DIR;
fs.mkdirSync(CACHE, { recursive: true });
fs.mkdirSync(OUT, { recursive: true });

const roadDef = JSON.parse(fs.readFileSync(path.join(ROOT, 'content/roads/quds-ariha.json'), 'utf8'));
const placesDef = JSON.parse(fs.readFileSync(path.join(ROOT, 'content/places.json'), 'utf8'));
const WATER_Y = toGameY(placesDef.deadSeaLevel);

// ---------------------------------------------------------------- grids
const NEAR = { cell: 3, cols: 1792, rows: 1152, x0: 0, z0: 0 };
const FAR = { cell: 36, cols: 384, rows: 352, x0: 0, z0: 0 };
{
  const pts: [number, number][] = [
    ...roadDef.waypoints.map((w: any) => toGame(w.lon, w.lat)),
    ...placesDef.places.map((p: any) => toGame(p.lon, p.lat)),
  ];
  const xs = pts.map((p) => p[0]), zs = pts.map((p) => p[1]);
  const cx = (Math.min(...xs) + Math.max(...xs)) / 2, cz = (Math.min(...zs) + Math.max(...zs)) / 2;
  const snap = (v: number, s: number) => Math.round(v / s) * s;
  NEAR.x0 = snap(cx - (NEAR.cols * NEAR.cell) / 2, FAR.cell);
  NEAR.z0 = snap(cz - (NEAR.rows * NEAR.cell) / 2, FAR.cell);
  FAR.x0 = snap(cx + 800 - (FAR.cols * FAR.cell) / 2, FAR.cell);
  FAR.z0 = snap(cz - (FAR.rows * FAR.cell) / 2, FAR.cell);
  console.log('content bbox x', Math.min(...xs) | 0, Math.max(...xs) | 0, 'z', Math.min(...zs) | 0, Math.max(...zs) | 0);
  console.log('near rect x', NEAR.x0, NEAR.x0 + NEAR.cols * NEAR.cell, 'z', NEAR.z0, NEAR.z0 + NEAR.rows * NEAR.cell);
}

// ---------------------------------------------------------------- elevation tiles
const ZOOM = 12, TILE = 256, N = 2 ** ZOOM;
const lonToPx = (lon: number) => ((lon + 180) / 360) * N * TILE;
const latToPy = (lat: number) => ((1 - Math.asinh(Math.tan((lat * Math.PI) / 180)) / Math.PI) / 2) * N * TILE;

async function loadDem() {
  const corners = [
    toReal(FAR.x0, FAR.z0), toReal(FAR.x0 + FAR.cols * FAR.cell, FAR.z0),
    toReal(FAR.x0, FAR.z0 + FAR.rows * FAR.cell), toReal(FAR.x0 + FAR.cols * FAR.cell, FAR.z0 + FAR.rows * FAR.cell),
  ];
  const lons = corners.map((c) => c[0]), lats = corners.map((c) => c[1]);
  const tx0 = Math.floor(lonToPx(Math.min(...lons)) / TILE) - 1, tx1 = Math.floor(lonToPx(Math.max(...lons)) / TILE) + 1;
  const ty0 = Math.floor(latToPy(Math.max(...lats)) / TILE) - 1, ty1 = Math.floor(latToPy(Math.min(...lats)) / TILE) + 1;
  const w = (tx1 - tx0 + 1) * TILE, h = (ty1 - ty0 + 1) * TILE;
  console.log(`DEM tiles x ${tx0}..${tx1} y ${ty0}..${ty1} (${(tx1 - tx0 + 1) * (ty1 - ty0 + 1)} tiles)`);
  let dem = new Float32Array(w * h);
  const jobs: (() => Promise<void>)[] = [];
  for (let ty = ty0; ty <= ty1; ty++) for (let tx = tx0; tx <= tx1; tx++) jobs.push(async () => {
    const file = path.join(CACHE, `${ZOOM}-${tx}-${ty}.png`);
    if (!fs.existsSync(file)) {
      const url = `https://s3.amazonaws.com/elevation-tiles-prod/terrarium/${ZOOM}/${tx}/${ty}.png`;
      for (let attempt = 0; ; attempt++) {
        try {
          const res = await fetch(url);
          if (!res.ok) throw new Error(`${res.status}`);
          fs.writeFileSync(file, Buffer.from(await res.arrayBuffer()));
          break;
        } catch (e) { if (attempt >= 3) throw new Error(`tile ${url}: ${e}`); await new Promise((r) => setTimeout(r, 1000 * 2 ** attempt)); }
      }
    }
    const { data, info } = await sharp(file).raw().toBuffer({ resolveWithObject: true });
    const ch = info.channels;
    for (let y = 0; y < TILE; y++) for (let x = 0; x < TILE; x++) {
      const i = (y * TILE + x) * ch;
      dem[(ty - ty0) * TILE * w + y * w + (tx - tx0) * TILE + x] = data[i] * 256 + data[i + 1] + data[i + 2] / 256 - 32768;
    }
  });
  let next = 0;
  await Promise.all(Array.from({ length: 8 }, async () => { while (next < jobs.length) await jobs[next++](); }));
  // The source data is in whole metres; a light blur removes the stair-steps.
  const k = [1, 4, 6, 4, 1].map((v) => v / 16);
  for (const horizontal of [true, false]) {
    const out = new Float32Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      let s = 0;
      for (let t = -2; t <= 2; t++) {
        const xx = horizontal ? Math.min(w - 1, Math.max(0, x + t)) : x, yy = horizontal ? y : Math.min(h - 1, Math.max(0, y + t));
        s += dem[yy * w + xx] * k[t + 2];
      }
      out[y * w + x] = s;
    }
    dem = out;
  }
  const ox = tx0 * TILE, oy = ty0 * TILE;
  const cr = (a: number, b: number, c: number, d: number, t: number) =>
    0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - 3 * c + d - a) * t * t * t);
  return (lon: number, lat: number): number => {
    const px = lonToPx(lon) - ox - 0.5, py = latToPy(lat) - oy - 0.5;
    const ix = Math.floor(px), iy = Math.floor(py), fx = px - ix, fy = py - iy;
    const row = (yy: number) => {
      const r = Math.min(h - 1, Math.max(0, yy)) * w, c = (xx: number) => dem[r + Math.min(w - 1, Math.max(0, xx))];
      return cr(c(ix - 1), c(ix), c(ix + 1), c(ix + 2), fx);
    };
    return cr(row(iy - 1), row(iy), row(iy + 1), row(iy + 2), fy);
  };
}

// ---------------------------------------------------------------- helpers
function hash2(x: number, y: number) { const s = Math.sin(x * 127.1 + y * 311.7) * 43758.5453; return s - Math.floor(s); }
function vnoise(x: number, y: number) {
  const ix = Math.floor(x), iy = Math.floor(y), fx = x - ix, fy = y - iy;
  const u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
  return (hash2(ix, iy) * (1 - u) + hash2(ix + 1, iy) * u) * (1 - v) + (hash2(ix, iy + 1) * (1 - u) + hash2(ix + 1, iy + 1) * u) * v;
}
const smoothstep = (a: number, b: number, x: number) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

class MinHeap {
  keys = new Float64Array(1 << 20); vals = new Int32Array(1 << 20); n = 0;
  push(k: number, v: number) {
    if (this.n === this.keys.length) { const K = new Float64Array(this.n * 2); K.set(this.keys); this.keys = K; const V = new Int32Array(this.n * 2); V.set(this.vals); this.vals = V; }
    let i = this.n++;
    while (i > 0) { const p = (i - 1) >> 1; if (this.keys[p] <= k) break; this.keys[i] = this.keys[p]; this.vals[i] = this.vals[p]; i = p; }
    this.keys[i] = k; this.vals[i] = v;
  }
  pop(): number {
    const top = this.vals[0]; const k = this.keys[--this.n], v = this.vals[this.n]; let i = 0;
    for (;;) { let c = 2 * i + 1; if (c >= this.n) break; if (c + 1 < this.n && this.keys[c + 1] < this.keys[c]) c++; if (this.keys[c] >= k) break; this.keys[i] = this.keys[c]; this.vals[i] = this.vals[c]; i = c; }
    this.keys[i] = k; this.vals[i] = v; return top;
  }
}

function encode(h: Float32Array, cols: number, rows: number, file: string) {
  let lo = Infinity, hi = -Infinity;
  for (const v of h) { if (v < lo) lo = v; if (v > hi) hi = v; }
  lo = Math.floor(lo) - 1; hi = Math.ceil(hi) + 1;
  const buf = Buffer.alloc(h.length * 2);
  for (let r = 0; r <= rows; r++) {
    let prev = 0;
    for (let c = 0; c <= cols; c++) {
      const q = Math.round(((h[r * (cols + 1) + c] - lo) / (hi - lo)) * 65535);
      buf.writeUInt16LE((q - prev) & 0xffff, (r * (cols + 1) + c) * 2); prev = q;
    }
  }
  const gz = zlib.gzipSync(buf, { level: 9 });
  fs.writeFileSync(path.join(OUT, file), gz);
  console.log(file, (gz.length / 1e6).toFixed(2), 'MB, y', lo, '..', hi);
  return { yMin: lo, yMax: hi };
}

// ---------------------------------------------------------------- main
const dem = await loadDem();
const height = (x: number, z: number) => { const [lon, lat] = toReal(x, z); return dem(lon, lat) / V_SCALE; };

console.log('sampling far terrain');
const fw = FAR.cols + 1, fh = FAR.rows + 1;
const far = new Float32Array(fw * fh);
for (let r = 0; r < fh; r++) for (let c = 0; c < fw; c++) far[r * fw + c] = height(FAR.x0 + c * FAR.cell, FAR.z0 + r * FAR.cell);
const farAt = (x: number, z: number) => {
  const gx = Math.min(FAR.cols - 1e-6, Math.max(0, (x - FAR.x0) / FAR.cell)), gz = Math.min(FAR.rows - 1e-6, Math.max(0, (z - FAR.z0) / FAR.cell));
  const ix = Math.floor(gx), iz = Math.floor(gz), fx = gx - ix, fz = gz - iz, i = iz * fw + ix;
  return (far[i] * (1 - fx) + far[i + 1] * fx) * (1 - fz) + (far[i + fw] * (1 - fx) + far[i + fw + 1] * fx) * fz;
};

console.log('sampling near terrain');
const nw = NEAR.cols + 1, nh = NEAR.rows + 1;
const near = new Float32Array(nw * nh);
for (let r = 0; r < nh; r++) for (let c = 0; c < nw; c++) {
  const x = NEAR.x0 + c * NEAR.cell, z = NEAR.z0 + r * NEAR.cell;
  let h = height(x, z);
  const edge = Math.min(c, r, NEAR.cols - c, NEAR.rows - r) * NEAR.cell;
  if (edge < 72) h = farAt(x, z) + (h - farAt(x, z)) * smoothstep(0, 72, edge);
  near[r * nw + c] = h;
}
// Fine relief the 30 m source data cannot hold: rougher on slopes, almost none on flats.
for (let r = 1; r < nh - 1; r++) for (let c = 1; c < nw - 1; c++) {
  const i = r * nw + c, x = NEAR.x0 + c * NEAR.cell, z = NEAR.z0 + r * NEAR.cell;
  const slope = Math.hypot(near[i + 1] - near[i - 1], near[i + nw] - near[i - nw]) / (2 * NEAR.cell);
  const edge = smoothstep(0, 72, Math.min(c, r, NEAR.cols - c, NEAR.rows - r) * NEAR.cell);
  const amp = (0.08 + Math.min(1, slope * 2.5) * 0.55) * edge;
  near[i] += amp * ((vnoise(x / 19, z / 19) - 0.5) * 1.6 + (vnoise(x / 7.3, z / 7.3) - 0.5) * 0.7);
}

// ---------------------------------------------------------------- route finding on a 6 m grid
console.log('routing road');
const G = 6, gw = NEAR.cols / 2, gh = NEAR.rows / 2;
let Hs = new Float32Array(gw * gh);
for (let r = 0; r < gh; r++) for (let c = 0; c < gw; c++) {
  const i = r * 2 * nw + c * 2;
  Hs[r * gw + c] = (near[i] + near[i + 1] + near[i + nw] + near[i + nw + 1]) / 4;
}
for (let pass = 0; pass < 2; pass++) for (const horizontal of [true, false]) {
  const out = new Float32Array(gw * gh);
  for (let r = 0; r < gh; r++) for (let c = 0; c < gw; c++) {
    let s = 0;
    for (let t = -2; t <= 2; t++) s += horizontal ? Hs[r * gw + Math.min(gw - 1, Math.max(0, c + t))] : Hs[Math.min(gh - 1, Math.max(0, r + t)) * gw + c];
    out[r * gw + c] = s / 5;
  }
  Hs = out;
}
const HsAt = (x: number, z: number) => {
  const gx = Math.min(gw - 1.001, Math.max(0, (x - NEAR.x0) / G - 0.5)), gz = Math.min(gh - 1.001, Math.max(0, (z - NEAR.z0) / G - 0.5));
  const ix = Math.floor(gx), iz = Math.floor(gz), fx = gx - ix, fz = gz - iz, i = iz * gw + ix;
  return (Hs[i] * (1 - fx) + Hs[i + 1] * fx) * (1 - fz) + (Hs[i + gw] * (1 - fx) + Hs[i + gw + 1] * fx) * fz;
};
const slopeMag = new Float32Array(gw * gh);
for (let r = 1; r < gh - 1; r++) for (let c = 1; c < gw - 1; c++) {
  const i = r * gw + c;
  slopeMag[i] = Math.hypot(Hs[i + 1] - Hs[i - 1], Hs[i + gw] - Hs[i - gw]) / (2 * G);
}
const cellOf = (x: number, z: number): [number, number] => [Math.round((x - NEAR.x0) / G - 0.5), Math.round((z - NEAR.z0) / G - 0.5)];
const cellXZ = (c: number, r: number): [number, number] => [NEAR.x0 + (c + 0.5) * G, NEAR.z0 + (r + 0.5) * G];

const wps: { id: string; x: number; z: number; stopAtWater?: boolean }[] = roadDef.waypoints.map((w: any) => {
  const [x, z] = toGame(w.lon, w.lat); return { id: w.id, x, z, stopAtWater: w.stopAtWater };
});
// The last leg ends where the straight line to its target first meets the 1910 shoreline.
{
  const a = wps[wps.length - 2], b = wps[wps.length - 1];
  if (b.stopAtWater) for (let t = 0; t <= 1; t += 0.002) {
    const x = a.x + (b.x - a.x) * t, z = a.z + (b.z - a.z) * t;
    if (HsAt(x, z) < WATER_Y + 1.2) { b.x = x; b.z = z; break; }
  }
}
const mask = new Uint8Array(gw * gh);
for (let r = 2; r < gh - 2; r++) for (let c = 2; c < gw - 2; c++) {
  const [x, z] = cellXZ(c, r);
  if (Hs[r * gw + c] < WATER_Y + 0.6) continue;
  for (let k = 0; k < wps.length - 1; k++) {
    const ax = wps[k].x, az = wps[k].z, bx = wps[k + 1].x - ax, bz = wps[k + 1].z - az;
    const t = Math.min(1, Math.max(0, ((x - ax) * bx + (z - az) * bz) / (bx * bx + bz * bz)));
    if (Math.hypot(x - ax - bx * t, z - az - bz * t) < 800) { mask[r * gw + c] = 1; break; }
  }
}
const DIRS: [number, number][] = [];
for (let dx = -2; dx <= 2; dx++) for (let dz = -2; dz <= 2; dz++) {
  if (Math.max(Math.abs(dx), Math.abs(dz)) === 1 || Math.abs(dx) + Math.abs(dz) === 3) DIRS.push([dx, dz]);
}
DIRS.sort((a, b) => Math.atan2(a[1], a[0]) - Math.atan2(b[1], b[0]));
const gScore = new Float32Array(gw * gh * 16), parent = new Int32Array(gw * gh * 16);

function astar(sc: number, sr: number, sdir: number, gc: number, gr: number, goalRad: number) {
  gScore.fill(Infinity);
  const heap = new MinHeap();
  const hfun = (c: number, r: number) => Math.max(0, Math.hypot(c - gc, r - gr) * G - goalRad);
  for (let d = 0; d < 16; d++) {
    if (sdir >= 0 && Math.min((d - sdir + 16) % 16, (sdir - d + 16) % 16) > 1) continue;
    const s = (sr * gw + sc) * 16 + d; gScore[s] = 0; parent[s] = -1; heap.push(hfun(sc, sr), s);
  }
  while (heap.n) {
    const s = heap.pop(), cell = s >> 4, d = s & 15, c = cell % gw, r = (cell / gw) | 0;
    if (hfun(c, r) <= 0) {
      const cells: number[] = []; let p = s;
      while (p >= 0) { cells.push(p >> 4); p = parent[p]; }
      return { cells: cells.reverse(), dir: d };
    }
    const g0 = gScore[s], h0 = Hs[cell];
    for (let dd = -1; dd <= 1; dd++) {
      const nd = (d + dd + 16) % 16, [dx, dz] = DIRS[nd], nc = c + dx, nr = r + dz, ncell = nr * gw + nc;
      if (!mask[ncell]) continue;
      if (Math.abs(dx) + Math.abs(dz) === 3 && !mask[(r + Math.round(dz / 2)) * gw + c + Math.round(dx / 2)]) continue;
      const len = Math.hypot(dx, dz) * G, grade = Math.abs(Hs[ncell] - h0) / len;
      if (grade > 0.5) continue;
      const cost = len * (1 + 3 * (grade / 0.1) ** 4 + 1.5 * Math.min(1, slopeMag[ncell] / 0.5)) + (dd ? 5 : 0);
      const ns = ncell * 16 + nd, ng = g0 + cost;
      if (ng < gScore[ns]) { gScore[ns] = ng; parent[ns] = s; heap.push(ng + hfun(nc, nr), ns); }
    }
  }
  throw new Error('no route found');
}

let cells: number[] = [];
{
  let [c, r] = cellOf(wps[0].x, wps[0].z), dir = -1;
  for (let k = 1; k < wps.length; k++) {
    const [gc, gr] = cellOf(wps[k].x, wps[k].z);
    const leg = astar(c, r, dir, gc, gr, wps[k].stopAtWater ? 12 : 48);
    cells.push(...(cells.length ? leg.cells.slice(1) : leg.cells));
    const last = leg.cells[leg.cells.length - 1]; c = last % gw; r = (last / gw) | 0; dir = leg.dir;
    console.log('  leg to', wps[k].id, leg.cells.length, 'cells');
  }
}

// ---------------------------------------------------------------- smooth the line
type P = [number, number];
let line: P[] = cells.filter((_, i) => i % 2 === 0 || i === cells.length - 1).map((cl) => cellXZ(cl % gw, (cl / gw) | 0));
const resample = (pts: P[], step: number): P[] => {
  const out: P[] = [pts[0]]; let carry = 0;
  for (let i = 1; i < pts.length; i++) {
    const [ax, az] = pts[i - 1], [bx, bz] = pts[i], len = Math.hypot(bx - ax, bz - az);
    let d = step - carry;
    while (d <= len) { out.push([ax + ((bx - ax) * d) / len, az + ((bz - az) * d) / len]); d += step; }
    carry = len - (d - step);
  }
  out.push(pts[pts.length - 1]);
  return out;
};
for (let it = 0; it < 2; it++) {
  const out: P[] = [line[0]];
  for (let i = 0; i < line.length - 1; i++) {
    const [ax, az] = line[i], [bx, bz] = line[i + 1];
    out.push([ax * 0.75 + bx * 0.25, az * 0.75 + bz * 0.25], [ax * 0.25 + bx * 0.75, az * 0.25 + bz * 0.75]);
  }
  out.push(line[line.length - 1]); line = out;
}
line = resample(line, 3);
for (let it = 0; it < 40; it++) {
  const out = line.map((p) => [...p] as P);
  for (let i = 1; i < line.length - 1; i++) {
    out[i][0] = line[i][0] * 0.5 + (line[i - 1][0] + line[i + 1][0]) * 0.25;
    out[i][1] = line[i][1] * 0.5 + (line[i - 1][1] + line[i + 1][1]) * 0.25;
  }
  line = out;
}
line = resample(line, 3);
const n = line.length;

// ---------------------------------------------------------------- vertical profile
let ys = new Float32Array(n);
for (let i = 0; i < n; i++) ys[i] = HsAt(line[i][0], line[i][1]);
const blur = (a: Float32Array, rad: number) => {
  const out = new Float32Array(a.length);
  for (let i = 0; i < a.length; i++) {
    let s = 0, w = 0;
    for (let t = -rad; t <= rad; t++) { const j = Math.min(a.length - 1, Math.max(0, i + t)), k = Math.exp((-t * t) / (0.5 * rad * rad)); s += a[j] * k; w += k; }
    out[i] = s / w;
  }
  return out;
};
ys = blur(ys, 10);
const MAX_GRADE = 0.13;
for (let it = 0; it < 30; it++) {
  const f = ys.slice(), b = ys.slice();
  for (let i = 1; i < n; i++) f[i] = Math.min(f[i - 1] + MAX_GRADE * 3, Math.max(f[i - 1] - MAX_GRADE * 3, f[i]));
  for (let i = n - 2; i >= 0; i--) b[i] = Math.min(b[i + 1] + MAX_GRADE * 3, Math.max(b[i + 1] - MAX_GRADE * 3, b[i]));
  for (let i = 0; i < n; i++) ys[i] = (f[i] + b[i]) / 2;
  ys = blur(ys, 4);
}
for (let i = 0; i < n; i++) ys[i] = Math.max(ys[i], WATER_Y + 0.9);

// ---------------------------------------------------------------- carve the road bed into the terrain
console.log('carving road bed');
const HW = roadDef.width / 2, SHOULDER = 24, RAD = HW + 1 + SHOULDER;
const bestD = new Float32Array(nw * nh).fill(Infinity), bestY = new Float32Array(nw * nh);
for (let i = 0; i < n - 1; i++) {
  const [ax, az] = line[i], [bx, bz] = line[i + 1], sx = bx - ax, sz = bz - az, l2 = sx * sx + sz * sz;
  const c0 = Math.max(0, Math.floor((Math.min(ax, bx) - RAD - NEAR.x0) / NEAR.cell)), c1 = Math.min(NEAR.cols, Math.ceil((Math.max(ax, bx) + RAD - NEAR.x0) / NEAR.cell));
  const r0 = Math.max(0, Math.floor((Math.min(az, bz) - RAD - NEAR.z0) / NEAR.cell)), r1 = Math.min(NEAR.rows, Math.ceil((Math.max(az, bz) + RAD - NEAR.z0) / NEAR.cell));
  for (let r = r0; r <= r1; r++) for (let c = c0; c <= c1; c++) {
    const x = NEAR.x0 + c * NEAR.cell, z = NEAR.z0 + r * NEAR.cell;
    const t = Math.min(1, Math.max(0, ((x - ax) * sx + (z - az) * sz) / l2));
    const d = Math.hypot(x - ax - sx * t, z - az - sz * t), k = r * nw + c;
    if (d < bestD[k]) { bestD[k] = d; bestY[k] = ys[i] + (ys[i + 1] - ys[i]) * t; }
  }
}
for (let k = 0; k < near.length; k++) {
  if (bestD[k] >= RAD) continue;
  const w = smoothstep(HW + 1, RAD, bestD[k]);
  near[k] = (bestY[k] - 0.3) * (1 - w) + near[k] * w;
}

// ---------------------------------------------------------------- stats and output
const dist = new Float32Array(n);
let maxGrade = 0, seaLevelAt = -1;
for (let i = 1; i < n; i++) {
  const ds = Math.hypot(line[i][0] - line[i - 1][0], line[i][1] - line[i - 1][1]);
  dist[i] = dist[i - 1] + ds;
  maxGrade = Math.max(maxGrade, Math.abs(ys[i] - ys[i - 1]) / ds);
  if (seaLevelAt < 0 && ys[i - 1] > 0 && ys[i] <= 0) seaLevelAt = dist[i];
}
const nearestOnRoad = (x: number, z: number) => {
  let best = Infinity, bi = 0;
  for (let i = 0; i < n; i++) { const d = Math.hypot(line[i][0] - x, line[i][1] - z); if (d < best) { best = d; bi = i; } }
  return { roadDist: Math.round(dist[bi]), offRoad: Math.round(best) };
};
const nearAt = (x: number, z: number) => {
  const gx = (x - NEAR.x0) / NEAR.cell, gz = (z - NEAR.z0) / NEAR.cell;
  if (gx < 0 || gz < 0 || gx >= NEAR.cols || gz >= NEAR.rows) return farAt(x, z);
  const ix = Math.floor(gx), iz = Math.floor(gz), fx = gx - ix, fz = gz - iz, i = iz * nw + ix;
  return (near[i] * (1 - fx) + near[i + 1] * fx) * (1 - fz) + (near[i + nw] * (1 - fx) + near[i + nw + 1] * fx) * fz;
};
const places = placesDef.places.map((p: any) => {
  const [x, z] = toGame(p.lon, p.lat);
  return { id: p.id, kind: p.kind, name: p.name, x: +x.toFixed(1), z: +z.toFixed(1), y: +nearAt(x, z).toFixed(1), ...nearestOnRoad(x, z) };
});
const r2 = (v: number) => Math.round(v * 100) / 100;
const world = {
  generated: 'by tools/build-terrain.ts; do not edit by hand',
  waterY: r2(WATER_Y),
  near: { ...NEAR, file: 'near.rhm', ...encode(near, NEAR.cols, NEAR.rows, 'near.rhm') },
  far: { ...FAR, file: 'far.rhm', ...encode(far, FAR.cols, FAR.rows, 'far.rhm') },
  road: {
    id: roadDef.id, name: roadDef.name, width: roadDef.width, length: Math.round(dist[n - 1]), seaLevelAt: Math.round(seaLevelAt),
    points: Array.from({ length: n }, (_, i) => [r2(line[i][0]), r2(ys[i]), r2(line[i][1])]).flat(),
  },
  places,
  walls: placesDef.oldCityWalls.map(([lon, lat]: number[]) => toGame(lon, lat).map(r2)),
};
fs.writeFileSync(path.join(OUT, 'world.json'), JSON.stringify(world));
console.log(`road: ${(dist[n - 1] / 1000).toFixed(2)} km, from ${(ys[0] * V_SCALE) | 0} m to ${(ys[n - 1] * V_SCALE) | 0} m true elevation, ` +
  `mean grade ${(((ys[0] - ys[n - 1]) / dist[n - 1]) * 100).toFixed(1)}%, max ${(maxGrade * 100).toFixed(1)}%, sea level at ${seaLevelAt | 0} m`);
for (const p of places) console.log(`  ${p.id.padEnd(18)} true elevation ${String(Math.round(p.y * V_SCALE)).padStart(5)} m, road km ${(p.roadDist / 1000).toFixed(2)}, ${p.offRoad} m from road`);

if (PREVIEW) {
  for (const [name, h, cols, rows, g] of [['near', near, NEAR.cols, NEAR.rows, NEAR], ['far', far, FAR.cols, FAR.rows, FAR]] as const) {
    const step = name === 'near' ? 2 : 1, w = Math.floor(cols / step), hh = Math.floor(rows / step), img = Buffer.alloc(w * hh * 3);
    for (let r = 0; r < hh; r++) for (let c = 0; c < w; c++) {
      const i = r * step * (cols + 1) + c * step, v = h[i];
      const sx = (h[i + step] - h[Math.max(0, i - step)]) / (2 * step * g.cell), sz = (h[Math.min(h.length - 1, i + step * (cols + 1))] - h[Math.max(0, i - step * (cols + 1))]) / (2 * step * g.cell);
      const shade = Math.min(1.3, Math.max(0.25, 0.75 - sx * 1.6 - sz * 1.6));
      const t = Math.min(1, Math.max(0, (v + 180) / 520));
      let col = v < WATER_Y ? [60, 110, 170] : [150 + 80 * t, 130 + 90 * t, 90 + 110 * t].map((q) => q * shade);
      if (Math.abs(v) < 0.6) col = [255, 255, 255];
      img.set(col.map((q) => Math.min(255, Math.max(0, q))), (r * w + c) * 3);
    }
    const dot = (x: number, z: number, rgb: number[], rad: number) => {
      const c = Math.round((x - g.x0) / g.cell / step), r = Math.round((z - g.z0) / g.cell / step);
      for (let a = -rad; a <= rad; a++) for (let b = -rad; b <= rad; b++) if (c + a >= 0 && c + a < w && r + b >= 0 && r + b < hh) img.set(rgb, ((r + b) * w + c + a) * 3);
    };
    for (const p of line) dot(p[0], p[1], [200, 30, 30], name === 'near' ? 1 : 0);
    for (const p of places) dot(p.x, p.z, [20, 20, 20], 3);
    for (const p of world.walls) dot(p[0], p[1], [120, 0, 160], 2);
    await sharp(img, { raw: { width: w, height: hh, channels: 3 } }).png().toFile(path.join(PREVIEW, `${name}.png`));
  }
}
