// Maps the real world (longitude, latitude, metres above sea level) to the compressed game world and back.
// Game axes: +x east, -z north, +y up, in metres. See DESIGN.md section 2.

export const ORIGIN = { lon: 35.2345, lat: 31.778 }; // Al-Quds, Old City
export const H_SCALE = 8; // horizontal compression of open country
export const V_SCALE = 2.5; // vertical compression

const M_PER_DEG_LAT = 110_950;
const M_PER_DEG_LON = 111_320 * Math.cos((ORIGIN.lat * Math.PI) / 180);

/** A place that is compressed less than the country around it, so it has room for streets. */
export interface Anchor {
  lon: number;
  lat: number;
  /** Magnification relative to open country (H_SCALE / mag is the local compression). */
  mag: number;
  /** Real-world radius (km) kept at full magnification. */
  innerKm: number;
  /** Real-world radius (km) at which the magnification has faded back to 1. */
  outerKm: number;
}

export const ANCHORS: Anchor[] = [
  { lon: 35.2345, lat: 31.778, mag: 3.2, innerKm: 0.8, outerKm: 2.4 }, // Al-Quds: about 1:2.5
  { lon: 35.459, lat: 31.856, mag: 2.2, innerKm: 0.9, outerKm: 2.2 }, // Ariha
];

interface Bubble { cx: number; cz: number; a: number; b: number; m: number }

function expand(rho: number, k: Bubble): number {
  const { a, b, m } = k;
  if (rho <= a) return m * rho;
  if (rho >= b) return m * b + (1 - m) * (b - a) * 0.5 + (rho - b);
  const t = (rho - a) / (b - a);
  return m * rho + (1 - m) * (b - a) * (t * t * t - 0.5 * t * t * t * t);
}

function contract(e: number, k: Bubble): number {
  if (e <= k.m * k.a) return e / k.m;
  const eb = expand(k.b, k);
  if (e >= eb) return k.b + (e - eb);
  let lo = k.a, hi = k.b;
  for (let i = 0; i < 40; i++) {
    const mid = 0.5 * (lo + hi);
    if (expand(mid, k) < e) lo = mid; else hi = mid;
  }
  return 0.5 * (lo + hi);
}

function applyBubble(x: number, z: number, k: Bubble, inverse: boolean): [number, number] {
  const dx = x - k.cx, dz = z - k.cz;
  const r = Math.hypot(dx, dz);
  if (r < 1e-9) return [x, z];
  const r2 = inverse ? contract(r, k) : expand(r, k);
  return [k.cx + (dx / r) * r2, k.cz + (dz / r) * r2];
}

function base(lon: number, lat: number): [number, number] {
  return [((lon - ORIGIN.lon) * M_PER_DEG_LON) / H_SCALE, (-(lat - ORIGIN.lat) * M_PER_DEG_LAT) / H_SCALE];
}

// Each bubble's centre is expressed in the space produced by the bubbles before it.
const BUBBLES: Bubble[] = [];
for (const an of ANCHORS) {
  let [cx, cz] = base(an.lon, an.lat);
  for (const k of BUBBLES) [cx, cz] = applyBubble(cx, cz, k, false);
  BUBBLES.push({ cx, cz, a: (an.innerKm * 1000) / H_SCALE, b: (an.outerKm * 1000) / H_SCALE, m: an.mag });
}

/** Real longitude/latitude to game x/z. */
export function toGame(lon: number, lat: number): [number, number] {
  let [x, z] = base(lon, lat);
  for (const k of BUBBLES) [x, z] = applyBubble(x, z, k, false);
  return [x, z];
}

/** Game x/z to real longitude/latitude. */
export function toReal(x: number, z: number): [number, number] {
  for (let i = BUBBLES.length - 1; i >= 0; i--) [x, z] = applyBubble(x, z, BUBBLES[i], true);
  return [ORIGIN.lon + (x * H_SCALE) / M_PER_DEG_LON, ORIGIN.lat - (z * H_SCALE) / M_PER_DEG_LAT];
}

/** Game metres per real metre at a point (about 1/8 in open country, larger inside cities). */
export function localScale(x: number, z: number): number {
  const [lon, lat] = toReal(x, z);
  const [x2, z2] = toGame(lon + 0.001, lat);
  return Math.hypot(x2 - x, z2 - z) / (0.001 * M_PER_DEG_LON);
}

export const toGameY = (realMetres: number) => realMetres / V_SCALE;
export const toRealElevation = (y: number) => y * V_SCALE;
