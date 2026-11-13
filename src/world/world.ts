// The drivable world: terrain heights, the road line and the exact ground surface the wheels roll on.
import { Heightmap, type GridInfo } from './heightmap';

export interface Place { id: string; kind: string; name: { en: string; ar: string }; x: number; y: number; z: number; roadDist: number; offRoad: number }
export interface WorldData {
  waterY: number;
  near: GridInfo & { file: string };
  far: GridInfo & { file: string };
  road: { id: string; name: { en: string; ar: string }; width: number; length: number; seaLevelAt: number; points: number[] };
  places: Place[];
  walls: [number, number][];
}

export type Surface = 'road' | 'ground';
export interface GroundHit { y: number; nx: number; ny: number; nz: number; surface: Surface }
export interface RoadHit { index: number; t: number; dist: number; y: number; along: number }

const smoothstep = (a: number, b: number, x: number) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

export class World {
  readonly n: number;
  /** Road centreline: x, y, z per point, and the distance along the road of each point. */
  readonly px: Float32Array; readonly py: Float32Array; readonly pz: Float32Array; readonly along: Float32Array;
  readonly halfWidth: number;
  readonly shoulder = 1.5;
  private readonly bucket = 12;
  private readonly buckets = new Map<number, number[]>();
  private readonly tmpN: [number, number, number] = [0, 1, 0];

  constructor(readonly data: WorldData, readonly near: Heightmap, readonly far: Heightmap) {
    const p = data.road.points;
    this.n = p.length / 3;
    this.px = new Float32Array(this.n); this.py = new Float32Array(this.n); this.pz = new Float32Array(this.n); this.along = new Float32Array(this.n);
    for (let i = 0; i < this.n; i++) {
      this.px[i] = p[i * 3]; this.py[i] = p[i * 3 + 1]; this.pz[i] = p[i * 3 + 2];
      if (i) this.along[i] = this.along[i - 1] + Math.hypot(this.px[i] - this.px[i - 1], this.pz[i] - this.pz[i - 1]);
      const key = this.key(this.px[i], this.pz[i]);
      let list = this.buckets.get(key);
      if (!list) this.buckets.set(key, (list = []));
      list.push(i);
    }
    this.halfWidth = data.road.width / 2;
  }

  get roadLength() { return this.along[this.n - 1]; }
  private key(x: number, z: number) { return Math.floor(x / this.bucket) * 100003 + Math.floor(z / this.bucket); }

  terrainY(x: number, z: number): number {
    return this.near.contains(x, z) ? this.near.at(x, z) : this.far.at(x, z);
  }

  /** Nearest point of the road centreline within about 12 m, or null. */
  nearestRoad(x: number, z: number): RoadHit | null {
    let best = Infinity, bi = -1;
    const bx = Math.floor(x / this.bucket), bz = Math.floor(z / this.bucket);
    for (let a = -1; a <= 1; a++) for (let b = -1; b <= 1; b++) {
      const list = this.buckets.get((bx + a) * 100003 + bz + b);
      if (!list) continue;
      for (const i of list) { const d = (this.px[i] - x) ** 2 + (this.pz[i] - z) ** 2; if (d < best) { best = d; bi = i; } }
    }
    if (bi < 0) return null;
    let out: RoadHit | null = null;
    for (const i of [bi - 1, bi]) {
      if (i < 0 || i >= this.n - 1) continue;
      const ax = this.px[i], az = this.pz[i], sx = this.px[i + 1] - ax, sz = this.pz[i + 1] - az;
      const t = Math.min(1, Math.max(0, ((x - ax) * sx + (z - az) * sz) / (sx * sx + sz * sz)));
      const dist = Math.hypot(x - ax - sx * t, z - az - sz * t);
      if (!out || dist < out.dist) out = { index: i, t, dist, y: this.py[i] + (this.py[i + 1] - this.py[i]) * t, along: this.along[i] + (this.along[i + 1] - this.along[i]) * t };
    }
    return out;
  }

  /** Position and unit direction of the road at a distance along it. */
  roadAt(s: number): { x: number; y: number; z: number; dx: number; dz: number; index: number } {
    s = Math.min(this.roadLength - 0.01, Math.max(0, s));
    let lo = 0, hi = this.n - 1;
    while (hi - lo > 1) { const mid = (lo + hi) >> 1; if (this.along[mid] <= s) lo = mid; else hi = mid; }
    const t = (s - this.along[lo]) / (this.along[hi] - this.along[lo]);
    const dx = this.px[hi] - this.px[lo], dz = this.pz[hi] - this.pz[lo], l = Math.hypot(dx, dz);
    return { x: this.px[lo] + dx * t, y: this.py[lo] + (this.py[hi] - this.py[lo]) * t, z: this.pz[lo] + dz * t, dx: dx / l, dz: dz / l, index: lo };
  }

  /** Height of the surface a wheel touches, ignoring the normal. */
  groundY(x: number, z: number): number {
    const ty = this.terrainY(x, z);
    const hit = this.nearestRoad(x, z);
    if (!hit || hit.dist > this.halfWidth + this.shoulder) return ty;
    return hit.y + (ty - hit.y) * smoothstep(this.halfWidth, this.halfWidth + this.shoulder, hit.dist);
  }

  ground(x: number, z: number, out: GroundHit): GroundHit {
    const hit = this.nearestRoad(x, z);
    if (hit && hit.dist <= this.halfWidth) {
      // On the road the surface is flat across and slopes along the road.
      const i = hit.index, sx = this.px[i + 1] - this.px[i], sz = this.pz[i + 1] - this.pz[i], sy = this.py[i + 1] - this.py[i];
      const l = Math.hypot(sx, sz), g = sy / l, k = 1 / Math.hypot(g, 1);
      out.y = hit.y; out.nx = (-g * sx) / l * k; out.ny = k; out.nz = (-g * sz) / l * k; out.surface = 'road';
      return out;
    }
    const e = 0.6;
    const dx = (this.groundY(x + e, z) - this.groundY(x - e, z)) / (2 * e), dz = (this.groundY(x, z + e) - this.groundY(x, z - e)) / (2 * e);
    const l = 1 / Math.hypot(dx, 1, dz);
    out.y = this.groundY(x, z); out.nx = -dx * l; out.ny = l; out.nz = -dz * l; out.surface = 'ground';
    return out;
  }

  /** The place nearest to a point, for the HUD. */
  nearestPlace(x: number, z: number): { place: Place; dist: number } {
    let best = Infinity, bp = this.data.places[0];
    for (const p of this.data.places) { const d = Math.hypot(p.x - x, p.z - z); if (d < best) { best = d; bp = p; } }
    return { place: bp, dist: best };
  }
}
