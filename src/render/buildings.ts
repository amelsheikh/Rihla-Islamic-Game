// Buildings: a small kit of textured shapes (walls, boxes, drums, domes, minarets) and the places built
// from it. Phase 1b covers the Al-Quds skyline, the villages, the khans, Maqam al-Nabi Musa and Ariha.
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import quds from '../../content/landmarks/al-quds.json';
import { toGame } from '../world/warp';
import type { World } from '../world/world';
import { applySunShade, type SunShade } from './sunshade';
import type { Photos } from './photo';
import { leadSheet, octagonFace, openings, plaster } from './textures';

type Mat = 'stone' | 'plaster' | 'lead' | 'tile' | 'opening' | 'gold';
const TEX: Record<Mat, number> = { stone: 3.2, plaster: 3, lead: 3, tile: 1, opening: 1, gold: 3 };
function rng(seed: number) { return () => ((seed = (seed * 16807) % 2147483647) / 2147483647); }

class Kit {
  private data = new Map<Mat, { pos: number[]; nrm: number[]; uv: number[]; col: number[] }>();
  constructor(private rapier: RAPIER.World | null) {}
  private buf(m: Mat) { let b = this.data.get(m); if (!b) this.data.set(m, (b = { pos: [], nrm: [], uv: [], col: [] })); return b; }

  /** A four-cornered face a,b,c,d (anticlockwise seen from outside) with texture coordinates. */
  quad(m: Mat, a: number[], b: number[], c: number[], d: number[], uv: number[][], tint: number[] = [1, 1, 1]) {
    const o = this.buf(m), ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = d[0] - a[0], vy = d[1] - a[1], vz = d[2] - a[2];
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx; const l = Math.hypot(nx, ny, nz) || 1; nx /= l; ny /= l; nz /= l;
    for (const i of [0, 1, 2, 0, 2, 3]) { const p = [a, b, c, d][i]; o.pos.push(p[0], p[1], p[2]); o.nrm.push(nx, ny, nz); o.uv.push(uv[i][0], uv[i][1]); o.col.push(tint[0], tint[1], tint[2]); }
  }

  /** A box standing at height `y`, turned by `yaw`. Solid boxes stop the car. */
  box(m: Mat, x: number, y: number, z: number, sx: number, sy: number, sz: number, yaw = 0, tint?: number[], solid = false, top: Mat = m) {
    const cs = Math.cos(yaw), sn = Math.sin(yaw), s = TEX[m];
    const P = (lx: number, ly: number, lz: number) => [x + lx * cs + lz * sn, y + ly, z - lx * sn + lz * cs];
    const hx = sx / 2, hz = sz / 2, u0 = (x + z) / s;
    const side = (a: number[], b: number[], w: number) => this.quad(m, P(a[0], 0, a[1]), P(b[0], 0, b[1]), P(b[0], sy, b[1]), P(a[0], sy, a[1]), [[u0, y / s], [u0 + w / s, y / s], [u0 + w / s, (y + sy) / s], [u0, (y + sy) / s]], tint);
    side([-hx, hz], [hx, hz], sx); side([hx, hz], [hx, -hz], sz); side([hx, -hz], [-hx, -hz], sx); side([-hx, -hz], [-hx, hz], sz);
    const t = TEX[top];
    this.quad(top, P(-hx, sy, hz), P(hx, sy, hz), P(hx, sy, -hz), P(-hx, sy, -hz), [[0, 0], [sx / t, 0], [sx / t, sz / t], [0, sz / t]], tint);
    if (solid && this.rapier) this.rapier.createCollider(RAPIER.ColliderDesc.cuboid(hx, sy / 2 + 1.5, hz).setTranslation(x, y + sy / 2 - 1.5, z).setRotation({ x: 0, y: Math.sin(yaw / 2), z: 0, w: Math.cos(yaw / 2) }));
  }

  /** An upright drum with `sides` faces. `faceTex` stretches one whole texture across each face. */
  drum(m: Mat, x: number, y: number, z: number, r: number, h: number, sides: number, tint?: number[], faceTex = false, r2 = r, cap: Mat | null = m) {
    const s = TEX[m], rot = Math.PI / sides;
    for (let i = 0; i < sides; i++) {
      const a0 = (i / sides) * 6.283 + rot, a1 = ((i + 1) / sides) * 6.283 + rot, w = 2 * r * Math.sin(Math.PI / sides);
      const p = (a: number, rr: number, yy: number) => [x + Math.sin(a) * rr, yy, z + Math.cos(a) * rr];
      const u0 = faceTex ? 0 : (i * w) / s, u1 = faceTex ? 1 : ((i + 1) * w) / s, v0 = faceTex ? 0 : y / s, v1 = faceTex ? 1 : (y + h) / s;
      this.quad(m, p(a0, r, y), p(a1, r, y), p(a1, r2, y + h), p(a0, r2, y + h), [[u0, v0], [u1, v0], [u1, v1], [u0, v1]], tint);
      if (cap) { const o = this.buf(cap); for (const q of [p(a0, r2, y + h), p(a1, r2, y + h), [x, y + h, z]]) { o.pos.push(q[0], q[1], q[2]); o.nrm.push(0, 1, 0); o.uv.push(q[0] / 3, q[2] / 3); o.col.push(...(tint ?? [1, 1, 1])); } }
    }
    if (m === 'tile' && this.rapier) this.rapier.createCollider(RAPIER.ColliderDesc.cylinder(h / 2 + 1, r).setTranslation(x, y + h / 2, z));
  }

  /** A dome: round, slightly pointed (the usual shape here) or onion-shaped. */
  dome(m: Mat, x: number, y: number, z: number, r: number, shape: 'round' | 'pointed' | 'onion' = 'pointed', tint?: number[], seg = 14) {
    const rings = seg > 12 ? 7 : 4, prof = (t: number): [number, number] => {
      const a = (t * Math.PI) / 2;
      if (shape === 'round') return [Math.cos(a), Math.sin(a)];
      if (shape === 'pointed') return [Math.pow(Math.cos(a), 0.82), Math.sin(a) * 1.12 + t * t * 0.1];
      return [Math.cos(a) * (1 + 0.42 * Math.sin(Math.PI * Math.min(1, t * 1.7))), Math.sin(a) * 1.5 + t * t * 0.3];
    };
    const o = this.buf(m), s = TEX[m], col = tint ?? [1, 1, 1];
    for (let j = 0; j < rings; j++) for (let i = 0; i < seg; i++) {
      const pts: number[][] = [], ns: number[][] = [], uvs: number[][] = [];
      for (const [di, dj] of [[0, 0], [1, 0], [1, 1], [0, 0], [1, 1], [0, 1]]) {
        const a = ((i + di) / seg) * 6.283, [pr, ph] = prof((j + dj) / rings), [nr, nh] = prof(Math.min(1, (j + dj) / rings + 0.02));
        pts.push([x + Math.sin(a) * pr * r, y + ph * r, z + Math.cos(a) * pr * r]);
        const tx = nr - pr, ty = nh - ph, l = Math.hypot(tx, ty) || 1; // outward normal is perpendicular to the profile
        ns.push([Math.sin(a) * (ty / l), -tx / l, Math.cos(a) * (ty / l)]);
        uvs.push([((i + di) / seg) * Math.max(1, Math.round((6.283 * r) / s)), ((j + dj) / rings) * Math.max(1, Math.round((1.6 * r) / s))]);
      }
      for (let k = 0; k < 6; k++) { o.pos.push(...pts[k]); o.nrm.push(...ns[k]); o.uv.push(...uvs[k]); o.col.push(...col); }
    }
  }

  /** A door or window drawn just in front of a wall. `kind`: 0 door, 1 arched window, 2 shuttered window, 3 great gate. */
  opening(kind: number, x: number, y: number, z: number, w: number, h: number, yaw: number) {
    const cs = Math.cos(yaw), sn = Math.sin(yaw), P = (lx: number, ly: number) => [x + lx * cs + 0.06 * sn, y + ly, z - lx * sn + 0.06 * cs];
    const u = (kind % 2) * 0.5, v = kind < 2 ? 0.5 : 0;
    this.quad('opening', P(-w / 2, 0), P(w / 2, 0), P(w / 2, h), P(-w / 2, h), [[u, v], [u + 0.5, v], [u + 0.5, v + 0.5], [u, v + 0.5]]);
  }

  /** A wall with battlements along a line, stepping with the ground. */
  rampart(world: World, pts: [number, number][], closed: boolean, h: number, thick: number) {
    for (let i = 0; i < pts.length - (closed ? 0 : 1); i++) {
      const [ax, az] = pts[i], [bx, bz] = pts[(i + 1) % pts.length], len = Math.hypot(bx - ax, bz - az), steps = Math.ceil(len / 9), yaw = Math.atan2(-(bz - az), bx - ax);
      for (let s = 0; s < steps; s++) {
        const t0 = s / steps, t1 = (s + 1) / steps, x = ax + (bx - ax) * (t0 + t1) / 2, z = az + (bz - az) * (t0 + t1) / 2, sl = len / steps;
        const base = Math.min(world.terrainY(ax + (bx - ax) * t0, az + (bz - az) * t0), world.terrainY(ax + (bx - ax) * t1, az + (bz - az) * t1), world.terrainY(x, z)) - 1.5;
        const top = world.terrainY(x, z) + h;
        this.box('stone', x, base, z, sl + 0.3, top - base, thick, yaw, undefined, true);
        const n = Math.max(2, Math.round(sl / 1.9));
        for (let k = 0; k < n; k++) { const o = ((k + 0.5) / n - 0.5) * sl; this.box('stone', x + o * Math.cos(yaw), top, z - o * Math.sin(yaw), 0.95, 0.9, thick * 0.45, yaw); }
      }
    }
  }
  tower(world: World, x: number, z: number, w: number, h: number, yaw = 0) {
    const y = world.terrainY(x, z) - 1.5;
    this.box('stone', x, y, z, w, h + 1.5, w, yaw, undefined, true);
    for (const [dx, dz] of [[-1, -1], [1, -1], [-1, 1], [1, 1], [0, -1], [0, 1], [-1, 0], [1, 0]]) this.box('stone', x + (dx * (w / 2 - 0.4)) , y + h + 1.5, z + dz * (w / 2 - 0.4), 0.9, 0.9, 0.9, yaw);
  }

  minaret(world: World, x: number, z: number, h: number, square: boolean) {
    const y = world.terrainY(x, z) - 1;
    if (square) {
      // Mamluk form: square shaft, a gallery, a smaller upper stage and a small dome.
      this.box('stone', x, y, z, 5, h * 0.68, 5, 0, undefined, true);
      this.box('stone', x, y + h * 0.68, z, 6.4, 0.7, 6.4);
      this.drum('stone', x, y + h * 0.68 + 0.7, z, 2.1, h * 0.2, 8);
      this.dome('lead', x, y + h * 0.88 + 0.7, z, 2.1, 'pointed');
    } else {
      // Ottoman form: slender round shaft, a gallery and a pointed cap.
      this.box('stone', x, y, z, 4.4, h * 0.3, 4.4, 0, undefined, true);
      this.drum('stone', x, y + h * 0.3, z, 1.7, h * 0.42, 12);
      this.drum('stone', x, y + h * 0.72, z, 2.4, 0.6, 12);
      this.drum('stone', x, y + h * 0.72 + 0.6, z, 1.3, h * 0.13, 12);
      this.drum('lead', x, y + h * 0.85 + 0.6, z, 1.45, h * 0.15, 12, undefined, false, 0.05, null);
    }
  }

  house(world: World, x: number, z: number, w: number, d: number, h: number, yaw: number, r: () => number, opts: { domed?: boolean; plain?: boolean; solid?: boolean }) {
    const g = world.terrainY(x, z), y = g - 1.5, k = 0.86 + r() * 0.2, tint = [k, k * (0.97 + r() * 0.03), k * (0.9 + r() * 0.08)];
    const m: Mat = opts.plain ? 'plaster' : 'stone';
    this.box(m, x, y, z, w, h + 1.5, d, yaw, tint, opts.solid ?? false, 'plaster');
    this.box(m, x, y + h + 1.5, z, w, 0.45, 0.35, yaw, tint); // a hint of a parapet on one edge
    if (opts.domed) this.dome('plaster', x, g + h, z, Math.min(w, d) * 0.32, 'round', [0.98, 0.97, 0.94], 8);
    const cs = Math.cos(yaw), sn = Math.sin(yaw);
    this.opening(0, x + (d / 2) * sn, g, z + (d / 2) * cs, 1.5, 2.4, yaw);
    if (w > 6) this.opening(1 + Math.floor(r() * 2), x + (d / 2) * sn + 2.2 * cs, g + 1.6, z + (d / 2) * cs - 2.2 * sn, 1.1, 1.5, yaw);
    this.opening(2, x + (w / 2) * cs, g + h - 2.2, z - (w / 2) * sn, 1.1, 1.4, yaw + Math.PI / 2);
  }

  build(shade: SunShade, photos: Photos): THREE.Group {
    const g = new THREE.Group(), st = { map: photos.ashlar.color, normalMap: photos.ashlar.normal }, pl = plaster(), ld = leadSheet(), oc = octagonFace(), op = openings();
    const mats: Record<Mat, THREE.MeshStandardMaterial> = {
      stone: new THREE.MeshStandardMaterial({ map: st.map, normalMap: st.normalMap, roughness: 0.9, vertexColors: true, color: 0xfff0d8 }),
      plaster: new THREE.MeshStandardMaterial({ map: pl.map, roughness: 0.85, vertexColors: true }),
      lead: new THREE.MeshStandardMaterial({ map: ld.map, roughness: 0.55, metalness: 0.35, vertexColors: true }),
      gold: new THREE.MeshStandardMaterial({ color: 0xd9a93a, roughness: 0.32, metalness: 0.9, vertexColors: true }),
      tile: new THREE.MeshStandardMaterial({ map: oc.map, roughness: 0.5, vertexColors: true }),
      opening: new THREE.MeshStandardMaterial({ map: op.map, roughness: 0.8, vertexColors: true, alphaTest: 0.5, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2 }),
    };
    for (const [m, b] of this.data) {
      const geo = new THREE.BufferGeometry();
      geo.setAttribute('position', new THREE.Float32BufferAttribute(b.pos, 3)); geo.setAttribute('normal', new THREE.Float32BufferAttribute(b.nrm, 3));
      geo.setAttribute('uv', new THREE.Float32BufferAttribute(b.uv, 2)); geo.setAttribute('color', new THREE.Float32BufferAttribute(b.col, 3));
      geo.computeBoundingSphere();
      applySunShade(mats[m], shade);
      const mesh = new THREE.Mesh(geo, mats[m]); mesh.castShadow = m !== 'opening'; mesh.receiveShadow = true; mesh.matrixAutoUpdate = false;
      g.add(mesh);
    }
    return g;
  }
}

export interface Named { x: number; y: number; z: number; en: string; ar: string; size: number }

/** Build every place in the corridor. Returns one group per place (so far-off places are skipped when drawing). */
export function buildPlaces(world: World, rapier: RAPIER.World | null, shade: SunShade, photos: Photos): { group: THREE.Group; named: Named[]; setDomeEra: (old: boolean) => void } {
  const root = new THREE.Group(), named: Named[] = [], P = (id: string) => world.data.places.find((p) => p.id === id)!, ty = (x: number, z: number) => world.terrainY(x, z);
  const r = rng(77);
  const clear = (x: number, z: number, m: number) => { const h = world.nearestRoad(x, z); return !h || h.dist > world.halfWidth + m; };
  const finish = (kit: Kit) => { const g = kit.build(shade, photos); root.add(g); return g; };
  let domeThen: THREE.Group | null = null, domeNow: THREE.Group | null = null;

  // ---------------------------------------------------------------- Al-Quds
  {
    const kit = new Kit(rapier), wall = world.data.walls;
    kit.rampart(world, wall, true, 9.5, 2.4);
    for (const [x, z] of wall) kit.tower(world, x, z, 6.5, 13);
    const inside = (poly: number[][], x: number, z: number) => {
      let c = false;
      for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) { const [xi, zi] = poly[i], [xj, zj] = poly[j]; if (zi > z !== zj > z && x < ((xj - xi) * (z - zi)) / (zj - zi) + xi) c = !c; }
      return c;
    };
    const haram = quds.haram.map(([lon, lat]) => toGame(lon, lat));
    // Bab al-Asbat: a gate tower in the east wall, with its arched gateway facing the Jericho road.
    {
      const g = P('bab-al-asbat');
      kit.tower(world, g.x, g.z, 10, 14);
      kit.opening(3, g.x + 5.05, ty(g.x + 6, g.z), g.z, 5.2, 7.5, Math.PI / 2);
    }
    // The Dome of the Rock on its raised platform: marble and tile octagon, drum, and the lead-covered dome of 1910.
    {
      const d = P('dome-of-the-rock'), y = ty(d.x, d.z);
      kit.box('stone', d.x, y - 1.5, d.z, 74, 3.1, 66, 0, [1.04, 1.03, 1], true, 'plaster');
      kit.drum('tile', d.x, y + 1.6, d.z, 19.5, 11.5, 8, undefined, true, 19.5, 'lead');
      kit.drum('tile', d.x, y + 13.1, d.z, 10.6, 6.2, 16, [0.8, 0.95, 1.05], false, 10.6, null);
      // The great dome is built twice: gilded as it is today, and lead-covered as it was around 1910.
      const now = new Kit(null), then = new Kit(null);
      now.dome('gold', d.x, y + 19.3, d.z, 10.9, 'pointed', undefined, 24); then.dome('lead', d.x, y + 19.3, d.z, 10.9, 'pointed', undefined, 24);
      domeNow = finish(now); domeThen = finish(then); domeThen.visible = false;
      kit.drum('gold', d.x, y + 19.3 + 12.6, d.z, 0.22, 3.2, 6);
      kit.dome('gold', d.x, y + 19.3 + 14, d.z, 0.55, 'round', undefined, 8);
      // The small Dome of the Chain to the east.
      kit.drum('stone', d.x + 27, y + 1.6, d.z, 4.6, 3.6, 11, undefined, false, 4.6, 'lead');
      kit.dome('lead', d.x + 27, y + 5.2, d.z, 3.2, 'pointed');
      named.push({ x: d.x, y: y + 44, z: d.z, en: d.name.en, ar: d.name.ar, size: 1 });
    }
    // Al-Qibli Mosque: a long gabled hall running north to south, with its dome at the southern end.
    {
      const q = P('al-qibli'), y = ty(q.x, q.z);
      kit.box('stone', q.x, y - 1.5, q.z, 34, 10.5, 46, 0, undefined, true, 'lead');
      kit.box('stone', q.x, y + 9, q.z, 10, 3.2, 46, 0, undefined, false, 'lead');
      for (let i = -3; i <= 3; i++) kit.opening(1, q.x + i * 4.4, y, q.z - 23, 3.2, 6, Math.PI);
      kit.drum('stone', q.x, y + 12.2, q.z + 16, 5.6, 3, 12, undefined, false, 5.6, null);
      kit.dome('lead', q.x, y + 15.2, q.z + 16, 5.9, 'pointed');
      named.push({ x: q.x, y: y + 34, z: q.z, en: q.name.en, ar: q.name.ar, size: 0.9 });
    }
    for (const s of quds.structures as any[]) {
      const [x, z] = toGame(s.lon, s.lat), y = ty(x, z);
      if (s.type === 'minaret-square' || s.type === 'minaret-round') kit.minaret(world, x, z, s.h, s.type === 'minaret-square');
      else if (s.type === 'sealed-gate') {
        // Bab al-Rahma: a double gate in the east wall, walled up for centuries.
        kit.box('stone', x + 1.5, y - 6, z, 7, 15, 17, 0, undefined, true);
        kit.opening(3, x + 5.05, y - 4, z - 3.6, 5.6, 8, Math.PI / 2); kit.opening(3, x + 5.05, y - 4, z + 3.6, 5.6, 8, Math.PI / 2);
        kit.dome('lead', x + 1.5, y + 9, z - 4, 2.6, 'round'); kit.dome('lead', x + 1.5, y + 9, z + 4, 2.6, 'round');
      } else if (s.type === 'church-domes') {
        kit.box('stone', x, y - 1.5, z, 30, 13, 26, 0.1, undefined, false, 'plaster');
        kit.drum('stone', x - 5, y + 11.5, z, 8.5, 4, 12, undefined, false, 8.5, null); kit.dome('lead', x - 5, y + 15.5, z, 8.7, 'round', undefined, 20);
        kit.drum('stone', x + 9, y + 11.5, z + 2, 5, 4.5, 12, undefined, false, 5, null); kit.dome('lead', x + 9, y + 16, z + 2, 5.1, 'round');
        kit.box('stone', x - 2, y - 1.5, z + 15, 6, 22, 6);
      } else if (s.type === 'bell-tower') {
        kit.box('plaster', x, y - 1.5, z, 6, s.h * 0.8, 6, 0.2, [1, 1, 0.98]);
        kit.drum('plaster', x, y + s.h * 0.8 - 1.5, z, 2.6, s.h * 0.1, 8);
        kit.drum('lead', x, y + s.h * 0.9 - 1.5, z, 2.9, s.h * 0.2, 8, undefined, false, 0.05, null);
      } else if (s.type === 'citadel') {
        for (const [dx, dz] of [[-12, -10], [12, -10], [-12, 10], [12, 10]]) kit.tower(world, x + dx, z + dz, 9, 16);
        kit.rampart(world, [[x - 12, z - 10], [x + 12, z - 10], [x + 12, z + 10], [x - 12, z + 10]], true, 10, 2);
        kit.minaret(world, x + 6, z + 12, 24, false);
      } else if (s.type === 'synagogue-dome') {
        kit.box('stone', x, y - 1.5, z, s.r * 2.6, 13, s.r * 2.6, 0.3, undefined, false, 'plaster');
        kit.drum('stone', x, y + 11.5, z, s.r, 3.5, 12, undefined, false, s.r, null); kit.dome('plaster', x, y + 15, z, s.r * 1.02, 'round', [0.9, 0.9, 0.88], 18);
      } else if (s.type === 'onion-church') {
        // The Russian church above Gethsemane, with its gilded onion domes.
        kit.box('plaster', x, y - 3, z, 13, 12, 17, 0.4, [1, 0.99, 0.95], true);
        for (const [dx, dz, rr, hh] of [[0, 0, 1.9, 6], [-4, -5, 1.2, 3], [4, -5, 1.2, 3], [-4, 5, 1.2, 3], [4, 5, 1.2, 3]]) {
          kit.drum('plaster', x + dx, y + 9, z + dz, rr * 0.8, hh, 8, undefined, false, rr * 0.8, null);
          kit.dome('gold', x + dx, y + 9 + hh, z + dz, rr, 'onion', undefined, 12);
        }
      }
      named.push({ x, y: y + 36, z, en: s.name.en, ar: s.name.ar, size: 0.62 });
    }
    // The houses of the Old City: closely packed stone cubes, many with small domes.
    const xs = wall.map((p) => p[0]), zs = wall.map((p) => p[1]);
    for (let x = Math.min(...xs); x < Math.max(...xs); x += 12.5) for (let z = Math.min(...zs); z < Math.max(...zs); z += 12.5) {
      const hx = x + (r() - 0.5) * 6, hz = z + (r() - 0.5) * 6;
      const edge = (m: number) => inside(wall, hx + m, hz) && inside(wall, hx - m, hz) && inside(wall, hx, hz + m) && inside(wall, hx, hz - m);
      if (!edge(9) || inside(haram, hx + 12, hz) || inside(haram, hx - 12, hz) || inside(haram, hx, hz + 12) || inside(haram, hx, hz - 12) || inside(haram, hx, hz)) continue;
      if ((quds.structures as any[]).some((s) => { const [sx, sz] = toGame(s.lon, s.lat); return Math.hypot(sx - hx, sz - hz) < 17; })) continue;
      kit.house(world, hx, hz, 8.5 + r() * 4.5, 8.5 + r() * 4.5, 6 + r() * 7, r() * 0.5, r, { domed: r() > 0.45 });
    }
    finish(kit);
  }

  // ---------------------------------------------------------------- villages, khans, Nabi Musa, Ariha
  const hamlet = (id: string, count: number, radius: number, plainShare: number) => {
    const kit = new Kit(rapier), p = P(id), placed: number[][] = [];
    for (let i = 0; i < count * 12 && placed.length < count; i++) {
      const a = r() * 6.283, d = Math.sqrt(r()) * radius, x = p.x + Math.cos(a) * d, z = p.z + Math.sin(a) * d;
      if (!clear(x, z, 7) || placed.some((q) => Math.hypot(q[0] - x, q[1] - z) < 13) || Math.abs(ty(x + 5, z) - ty(x - 5, z)) + Math.abs(ty(x, z + 5) - ty(x, z - 5)) > 5) continue;
      const h = world.nearestRoad(x, z), yaw = h ? Math.atan2(world.px[h.index] - x, world.pz[h.index] - z) : r() * 6.283;
      kit.house(world, x, z, 7 + r() * 4, 6.5 + r() * 3, 3.6 + r() * 2.4, yaw + (r() - 0.5) * 0.6, r, { domed: r() > 0.6, plain: r() < plainShare, solid: true });
      placed.push([x, z]);
    }
    finish(kit);
  };
  hamlet('al-eizariya', 30, 95, 0.2);
  hamlet('ariha', 80, 250, 0.7);
  {
    // A small mosque for Ariha.
    const kit = new Kit(rapier), p = P('ariha'); let x = p.x + 30, z = p.z + 10;
    for (let i = 0; i < 40 && !clear(x, z, 16); i++) { x += 6; z += 3; }
    const y = ty(x, z);
    kit.box('plaster', x, y - 1.5, z, 15, 7.5, 15, 0, undefined, true); kit.dome('plaster', x, y + 6, z, 5.2, 'pointed', [0.98, 0.98, 0.95]);
    kit.opening(0, x, y, z + 7.5, 2.2, 3.4, 0); kit.minaret(world, x + 10, z - 6, 19, false);
    finish(kit);
  }
  const khan = (id: string) => {
    // A roadside inn: four ranges around a courtyard, with one great gate facing the road.
    const kit = new Kit(rapier), p = P(id), h = world.nearestRoad(p.x, p.z), s = 32;
    const yaw = h ? Math.atan2(world.px[h.index] - p.x, world.pz[h.index] - p.z) : 0, cs = Math.cos(yaw), sn = Math.sin(yaw), y = ty(p.x, p.z) - 1.8;
    const L = (lx: number, lz: number): [number, number] => [p.x + lx * cs + lz * sn, p.z - lx * sn + lz * cs];
    for (const [lx, lz, sx, sz] of [[0, s / 2 - 3, s, 6], [0, -s / 2 + 3, s, 6], [-s / 2 + 3, 0, 6, s - 12], [s / 2 - 3, 0, 6, s - 12]]) { const [x, z] = L(lx, lz); kit.box('stone', x, y, z, sx, 8.3, sz, yaw, undefined, true, 'plaster'); }
    for (const [lx, lz] of [[-s / 2, s / 2], [s / 2, s / 2], [-s / 2, -s / 2], [s / 2, -s / 2]]) { const [x, z] = L(lx, lz); kit.tower(world, x, z, 5, 8.5, yaw); }
    const [gx, gz] = L(0, s / 2); kit.opening(3, gx, y + 1.8, gz, 4.6, 5.6, yaw);
    for (const o of [-10, -5, 5, 10]) { const [wx, wz] = L(o, s / 2); kit.opening(1, wx, y + 5, wz, 1.2, 1.6, yaw); }
    finish(kit);
    return L(0, s / 2 + 9);
  };
  khan('khan-al-ahmar');
  const pageSpot = khan('khan-al-hatruri');
  {
    // Maqam al-Nabi Musa: a walled compound roofed with many small white domes, and a minaret.
    const kit = new Kit(rapier), p = P('nabi-musa'), y = ty(p.x, p.z) - 1.8, s = 54;
    kit.box('plaster', p.x, y, p.z, s, 8.8, s, 0, [1, 0.99, 0.95], true);
    for (let i = 0; i < 6; i++) for (let j = 0; j < 6; j++) if ((i + j) % 2 === 0 || i === 0 || j === 5) kit.dome('plaster', p.x - s / 2 + 4.5 + i * 9, y + 8.8, p.z - s / 2 + 4.5 + j * 9, 3.3, 'round', [1, 1, 0.98], 10);
    kit.dome('plaster', p.x + 8, y + 8.8, p.z + 8, 6, 'pointed', [1, 1, 0.98]);
    kit.minaret(world, p.x + s / 2 - 5, p.z - s / 2 + 5, 24, false);
    kit.opening(3, p.x, y + 1.8, p.z + s / 2, 4.4, 5.6, 0);
    finish(kit);
  }
  for (const p of world.data.places) if (p.id !== 'dome-of-the-rock' && p.id !== 'al-qibli') named.push({ x: p.x, y: p.y + (p.kind === 'city' ? 62 : 26), z: p.z, en: p.name.en, ar: p.name.ar, size: p.kind === 'city' ? 1.2 : 0.9 });
  void pageSpot;
  return { group: root, named, setDomeEra: (old) => { if (domeNow && domeThen) { domeNow.visible = !old; domeThen.visible = old; } } };
}
