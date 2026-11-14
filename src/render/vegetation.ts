// Trees, shrubs, rocks and kerb stones: where they stand, what they look like, and which are drawn.
// All shapes are built by code. Plants are grouped into square cells so that far cells can be drawn
// with simpler shapes or skipped; beyond that, a dark dot in the ground colour stands in for each tree.
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { World } from '../world/world';
import { applySunShade, type SunShade } from './sunshade';

export type Species = 'olive' | 'cypress' | 'palm' | 'shrub' | 'rock' | 'kerb';
export interface Plant { kind: Species; x: number; y: number; z: number; scale: number; yaw: number; tint: number }

function rng(seed: number) { return () => ((seed = (seed * 16807) % 2147483647) / 2147483647); }
function hash3(x: number, y: number, z: number) { const s = Math.sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453; return s - Math.floor(s); }
function vnoise(x: number, y: number) {
  const ix = Math.floor(x), iy = Math.floor(y), fx = x - ix, fy = y - iy, h = (a: number, b: number) => hash3(a, b, 1.7);
  const u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
  return (h(ix, iy) * (1 - u) + h(ix + 1, iy) * u) * (1 - v) + (h(ix, iy + 1) * (1 - u) + h(ix + 1, iy + 1) * u) * v;
}
const sstep = (a: number, b: number, x: number) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

// ------------------------------------------------------------------ placement
export function placePlants(world: World): Plant[] {
  const out: Plant[] = [], r = rng(2024), ni = world.near.info, W = ni.cols * ni.cell, D = ni.rows * ni.cell;
  const P = (id: string) => world.data.places.find((p) => p.id === id)!;
  const wall = world.data.walls;
  const inCity = (x: number, z: number) => {
    let c = false;
    for (let i = 0, j = wall.length - 1; i < wall.length; j = i++) {
      const [xi, zi] = wall[i], [xj, zj] = wall[j];
      if (zi > z !== zj > z && x < ((xj - xi) * (z - zi)) / (zj - zi) + xi) c = !c;
    }
    return c;
  };
  const slope = (x: number, z: number) => Math.hypot(world.terrainY(x + 3, z) - world.terrainY(x - 3, z), world.terrainY(x, z + 3) - world.terrainY(x, z - 3)) / 6;
  const roadClear = (x: number, z: number, m: number) => { const h = world.nearestRoad(x, z); return !h || h.dist > world.halfWidth + m; };
  const add = (kind: Species, x: number, z: number, scale: number, margin: number) => {
    if (x < ni.x0 + 40 || z < ni.z0 + 40 || x > ni.x0 + W - 40 || z > ni.z0 + D - 40) return false;
    const y = world.terrainY(x, z);
    if (y < world.data.waterY + 0.8 || !roadClear(x, z, margin)) return false;
    out.push({ kind, x, y, z, scale, yaw: r() * 6.283, tint: r() });
    return true;
  };
  const arid = (x: number, z: number) => sstep(350, 1500, x + (vnoise(x / 90, z / 90) - 0.5) * 500);
  const ariha = P('ariha'), dome = P('dome-of-the-rock');

  // Olive groves on the western hills, in patches, thinning out towards the wilderness.
  for (let i = 0; i < 17000; i++) {
    const x = ni.x0 + 60 + r() * 2300, z = ni.z0 + 60 + r() * (D - 120);
    if (inCity(x, z) || r() < arid(x, z) * 1.25 || vnoise(x / 130 + 9, z / 130) < 0.42 || slope(x, z) > 0.42) continue;
    add('olive', x, z, 0.8 + r() * 0.55, 3);
  }
  // Cypresses: around the Haram esplanade, outside the walls, and beside the villages.
  for (let i = 0; i < 70; i++) { const a = r() * 6.283, d = 38 + r() * 70; add('cypress', dome.x + Math.cos(a) * d, dome.z + Math.sin(a) * d * 1.2, 0.8 + r() * 0.6, 2); }
  for (let i = 0; i < 2600; i++) {
    const x = ni.x0 + 60 + r() * 1500, z = ni.z0 + 60 + r() * (D - 120);
    if (inCity(x, z) || vnoise(x / 60 + 3, z / 60 + 5) < 0.66 || arid(x, z) > 0.5 || slope(x, z) > 0.5) continue;
    add('cypress', x, z, 0.75 + r() * 0.7, 3);
  }
  // Date palms of the Ariha oasis.
  for (let i = 0; i < 1800; i++) {
    const a = r() * 6.283, d = Math.sqrt(r()) * 520, x = ariha.x + Math.cos(a) * d, z = ariha.z + Math.sin(a) * d;
    if (d > 300 + vnoise(x / 70, z / 70) * 260) continue;
    add('palm', x, z, 0.8 + r() * 0.5, 3.5);
  }
  // Desert shrubs: sparse everywhere east of the ridge, thicker in the wadi beds.
  for (let i = 0; i < 42000; i++) {
    const x = ni.x0 + 60 + r() * (W - 120), z = ni.z0 + 60 + r() * (D - 120);
    if (inCity(x, z)) continue;
    const y = world.terrainY(x, z), hollow = (world.terrainY(x + 9, z) + world.terrainY(x - 9, z) + world.terrainY(x, z + 9) + world.terrainY(x, z - 9)) / 4 - y;
    if (r() > 0.1 + sstep(0.15, 1.2, hollow) * 0.75 || slope(x, z) > 0.75) continue;
    add('shrub', x, z, 0.5 + r() * 0.8, 1.2);
  }
  // Rocks: on steep ground and along the roadside.
  for (let i = 0; i < 34000; i++) {
    const x = ni.x0 + 60 + r() * (W - 120), z = ni.z0 + 60 + r() * (D - 120);
    if (inCity(x, z) || r() > sstep(0.25, 0.8, slope(x, z)) * 0.6 + 0.04) continue;
    add('rock', x, z, 0.35 + r() * r() * 1.6, 1.5);
  }
  return out;
}

// ------------------------------------------------------------------ shapes
type Col = [number, number, number];
const srgb = (hex: number): Col => { const c = new THREE.Color(hex); return [c.r, c.g, c.b]; };

/** A lumpy ball of foliage or stone, shaded lighter on top. */
function blob(cx: number, cy: number, cz: number, rx: number, ry: number, rz: number, detail: number, top: Col, bottom: Col, rough: number, seed: number) {
  const g = new THREE.IcosahedronGeometry(1, detail), p = g.attributes.position as THREE.BufferAttribute;
  const col = new Float32Array(p.count * 3), nrm = new Float32Array(p.count * 3);
  for (let i = 0; i < p.count; i++) {
    const x = p.getX(i), y = p.getY(i), z = p.getZ(i), k = 1 + (hash3(Math.round(x * 50) + seed, Math.round(y * 50), Math.round(z * 50)) - 0.5) * rough;
    p.setXYZ(i, cx + x * rx * k, cy + y * ry * k, cz + z * rz * k);
    nrm.set([x, y, z], i * 3);
    const t = y * 0.5 + 0.5, shade = 0.9 + hash3(x * 9 + seed, y * 9, z * 9) * 0.2;
    col.set([(bottom[0] + (top[0] - bottom[0]) * t) * shade, (bottom[1] + (top[1] - bottom[1]) * t) * shade, (bottom[2] + (top[2] - bottom[2]) * t) * shade], i * 3);
  }
  g.setAttribute('normal', new THREE.BufferAttribute(nrm, 3));
  g.setAttribute('color', new THREE.BufferAttribute(col, 3));
  g.deleteAttribute('uv');
  return g;
}
/** A tapering limb from a to b. */
function limb(a: THREE.Vector3, b: THREE.Vector3, ra: number, rb: number, sides: number, c: Col) {
  const g = new THREE.CylinderGeometry(rb, ra, a.distanceTo(b), sides, 1, true).toNonIndexed();
  g.applyMatrix4(new THREE.Matrix4().compose(a.clone().add(b).multiplyScalar(0.5), new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), b.clone().sub(a).normalize()), new THREE.Vector3(1, 1, 1)));
  const n = g.attributes.position.count, col = new Float32Array(n * 3);
  for (let i = 0; i < n; i++) col.set(c, i * 3);
  g.setAttribute('color', new THREE.BufferAttribute(col, 3)); g.deleteAttribute('uv');
  return g;
}
const V = (x: number, y: number, z: number) => new THREE.Vector3(x, y, z);
const BARK = srgb(0x5d4f40), OLIVE_T = srgb(0x9aa878), OLIVE_B = srgb(0x4c5a38);

function oliveHi() {
  const parts = [limb(V(0, -0.3, 0), V(0.1, 1.1, 0.05), 0.34, 0.24, 6, BARK), limb(V(0.1, 1.1, 0.05), V(0.9, 2.1, 0.2), 0.2, 0.1, 4, BARK), limb(V(0.1, 1.1, 0.05), V(-0.7, 2.2, -0.4), 0.2, 0.1, 4, BARK), limb(V(0.1, 1.1, 0.05), V(0, 2.3, 0.7), 0.17, 0.09, 4, BARK)];
  const b: [number, number, number, number][] = [[1.0, 2.7, 0.2, 1.25], [-0.9, 2.8, -0.5, 1.2], [0, 2.9, 0.9, 1.15], [0.1, 3.5, 0, 1.3], [-0.2, 2.5, -1.2, 0.9], [1.2, 2.4, -0.9, 0.85]];
  b.forEach(([x, y, z, s], i) => parts.push(blob(x, y, z, s * 1.15, s * 0.8, s * 1.15, 0, OLIVE_T, OLIVE_B, 0.45, i * 7)));
  return mergeGeometries(parts);
}
function oliveLo() { return mergeGeometries([limb(V(0, -0.3, 0), V(0, 1.6, 0), 0.3, 0.2, 3, BARK), blob(0, 2.9, 0, 2.2, 1.4, 2.2, 0, OLIVE_T, OLIVE_B, 0.35, 3)]); }
function cypress(hi: boolean) {
  const T = srgb(0x4e6a44), B = srgb(0x1f3324), parts = [limb(V(0, -0.3, 0), V(0, 1, 0), 0.16, 0.12, 5, BARK)];
  const n = hi ? 5 : 2;
  for (let i = 0; i < n; i++) { const t = i / n, w = 0.95 * (1 - t * 0.78); parts.push(blob(0, 1.4 + t * 7.2 + 0.8, 0, w, hi ? 1.5 : 3.6, w, 0, T, B, 0.3, i)); }
  return mergeGeometries(parts);
}
function palm(hi: boolean) {
  const TR = srgb(0x7a6a52), FT = srgb(0x7f9a4c), FB = srgb(0x3d5a2a), parts: THREE.BufferGeometry[] = [];
  const H = 8, seg = hi ? 5 : 2, bend = (t: number) => V(Math.sin(t * 1.1) * 0.9, t * H, 0);
  for (let i = 0; i < seg; i++) parts.push(limb(bend(i / seg).setY((i / seg) * H - (i ? 0 : 0.3)), bend((i + 1) / seg), 0.26 - (i / seg) * 0.07, 0.25 - ((i + 1) / seg) * 0.07, hi ? 6 : 3, TR));
  const top = bend(1), fronds = hi ? 12 : 6, fs = hi ? 3 : 2, pos: number[] = [], col: number[] = [], nrm: number[] = [];
  for (let f = 0; f < fronds; f++) {
    const a = (f / fronds) * 6.283 + (f % 2) * 0.3, lift = 0.75 - (f % 3) * 0.3, len = 3.6 + (f % 2) * 0.7, dx = Math.cos(a), dz = Math.sin(a);
    const pt = (t: number) => V(top.x + dx * len * t, top.y + (lift * t - 1.25 * t * t) * len * 0.62, top.z + dz * len * t);
    for (let s = 0; s < fs; s++) {
      const t0 = s / fs, t1 = (s + 1) / fs, p0 = pt(t0), p1 = pt(t1), w0 = 0.55 * Math.sin(Math.PI * (0.15 + t0 * 0.85)), w1 = 0.55 * Math.sin(Math.PI * (0.15 + t1 * 0.85));
      const q = [V(p0.x - dz * w0, p0.y - 0.12, p0.z + dx * w0), V(p0.x + dz * w0, p0.y - 0.12, p0.z - dx * w0), V(p1.x - dz * w1, p1.y - 0.12, p1.z + dx * w1), V(p1.x + dz * w1, p1.y - 0.12, p1.z - dx * w1)];
      for (const tri of [[p0, q[0], p1], [q[0], q[2], p1], [p0, p1, q[1]], [q[1], p1, q[3]]]) {
        for (const v of tri) { pos.push(v.x, v.y, v.z); nrm.push(0, 1, 0); const k = 1 - (v === p0 ? t0 : t1) * 0.35; col.push((FB[0] + (FT[0] - FB[0]) * 0.7) * k, (FB[1] + (FT[1] - FB[1]) * 0.7) * k, (FB[2] + (FT[2] - FB[2]) * 0.7) * k); }
      }
    }
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('normal', new THREE.Float32BufferAttribute(nrm, 3)); g.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
  parts.push(g, blob(top.x, top.y - 0.1, top.z, 0.45, 0.4, 0.45, 0, srgb(0x8a6a3a), TR, 0.2, 1));
  return mergeGeometries(parts);
}
function shrub() { const T = srgb(0x93966a), B = srgb(0x4f5238); return blob(0, 0.32, 0, 0.8, 0.5, 0.7, 0, T, B, 0.8, 1); }
function rock() { const g = blob(0, 0.25, 0, 1, 0.62, 0.85, 0, srgb(0xd0c4a2), srgb(0x8a7c62), 0.7, 5); g.computeVertexNormals(); return g; }
function kerb() {
  const g = new THREE.BoxGeometry(0.5, 0.3, 0.24).toNonIndexed(); g.translate(0, 0.08, 0);
  const n = g.attributes.position.count, col = new Float32Array(n * 3).fill(0.5);
  g.setAttribute('color', new THREE.BufferAttribute(col, 3)); g.deleteAttribute('uv');
  return g;
}

// ------------------------------------------------------------------ drawing
const SPEC: Record<Species, { range: number; hiRange: number; shadow: boolean; tintA: number; tintB: number }> = {
  olive: { range: 1400, hiRange: 120, shadow: true, tintA: 0xffffff, tintB: 0xc8d8b0 },
  cypress: { range: 1600, hiRange: 120, shadow: true, tintA: 0xffffff, tintB: 0xb8d0b0 },
  palm: { range: 1600, hiRange: 110, shadow: true, tintA: 0xffffff, tintB: 0xd0e0a8 },
  shrub: { range: 340, hiRange: 0, shadow: false, tintA: 0xffffff, tintB: 0xc0b890 },
  rock: { range: 420, hiRange: 0, shadow: false, tintA: 0xffffff, tintB: 0xd8c8a0 },
  kerb: { range: 260, hiRange: 0, shadow: false, tintA: 0xffffff, tintB: 0xe0d8c0 },
};
const CELL = 384;

export class Vegetation {
  readonly group = new THREE.Group();
  lodScale = 1;
  private cells: { x: number; z: number; kind: Species; hi: THREE.InstancedMesh | null; lo: THREE.InstancedMesh }[] = [];

  constructor(world: World, plants: Plant[], shade: SunShade, rapier: RAPIER.World | null) {
    const mat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.92, side: THREE.DoubleSide });
    applySunShade(mat, shade);
    const geo: Record<Species, { hi: THREE.BufferGeometry | null; lo: THREE.BufferGeometry }> = {
      olive: { hi: oliveHi(), lo: oliveLo() }, cypress: { hi: cypress(true), lo: cypress(false) }, palm: { hi: palm(true), lo: palm(false) },
      shrub: { hi: null, lo: shrub() }, rock: { hi: null, lo: rock() }, kerb: { hi: null, lo: kerb() },
    };
    const groups = new Map<string, Plant[]>();
    for (const p of plants) {
      const key = `${p.kind}|${Math.floor(p.x / CELL)}|${Math.floor(p.z / CELL)}`;
      let list = groups.get(key); if (!list) groups.set(key, (list = []));
      list.push(p);
    }
    const m = new THREE.Matrix4(), q = new THREE.Quaternion(), up = new THREE.Vector3(0, 1, 0), s = new THREE.Vector3(), pos = new THREE.Vector3(), c = new THREE.Color(), cb = new THREE.Color();
    for (const [key, list] of groups) {
      const [kind, cx, cz] = key.split('|') as [Species, string, string], spec = SPEC[kind];
      const make = (g: THREE.BufferGeometry, shadow: boolean) => {
        const im = new THREE.InstancedMesh(g, mat, list.length);
        list.forEach((p, i) => {
          const sq = kind === 'rock' ? 0.7 + p.tint * 0.6 : 1;
          m.compose(pos.set(p.x, p.y, p.z), q.setFromAxisAngle(up, p.yaw), s.set(p.scale, p.scale * sq, p.scale));
          im.setMatrixAt(i, m); im.setColorAt(i, c.setHex(spec.tintA).lerp(cb.setHex(spec.tintB), p.tint));
        });
        im.castShadow = shadow; im.receiveShadow = true; im.computeBoundingSphere(); im.visible = false; im.matrixAutoUpdate = false;
        this.group.add(im);
        return im;
      };
      this.cells.push({ x: (+cx + 0.5) * CELL, z: (+cz + 0.5) * CELL, kind, hi: geo[kind].hi ? make(geo[kind].hi!, spec.shadow) : null, lo: make(geo[kind].lo, false) });
    }
    // Trees and large rocks close to the road are solid.
    if (rapier) for (const p of plants) {
      if (p.kind === 'shrub' || p.kind === 'kerb' || (p.kind === 'rock' && p.scale < 0.9)) continue;
      const h = world.nearestRoad(p.x, p.z);
      if (!h || h.dist > 14) continue;
      rapier.createCollider(p.kind === 'rock' ? RAPIER.ColliderDesc.ball(p.scale * 0.6).setTranslation(p.x, p.y + 0.15, p.z) : RAPIER.ColliderDesc.cylinder(2, 0.28 * p.scale).setTranslation(p.x, p.y + 1.6, p.z));
    }
  }

  update(cam: THREE.Vector3) {
    for (const c of this.cells) {
      const spec = SPEC[c.kind], d = Math.max(0, Math.hypot(c.x - cam.x, c.z - cam.z) - CELL * 0.71);
      const near = c.hi !== null && d < spec.hiRange * this.lodScale;
      if (c.hi) c.hi.visible = near;
      c.lo.visible = !near && d < spec.range * this.lodScale;
    }
  }
}
