// The car: a modern family SUV, shaped by code from a handful of cross-sections and finished in metallic
// paint with a clear coat. It carries no maker's badge; real makes need a licence.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { CAR, type Vehicle } from '../physics/vehicle';

// Cross-sections from nose to tail: z, half-width, underside, shoulder line, top, half-width at the top.
// The outline is a sleek electric crossover: short nose with no grille, one arc from windscreen to tail.
const STATIONS = [
  [-2.36, 0.66, -0.3, -0.04, 0.03, 0.5], [-2.26, 0.86, -0.43, 0.07, 0.13, 0.72], [-1.8, 0.95, -0.47, 0.25, 0.3, 0.83],
  [-1.1, 0.96, -0.47, 0.36, 0.41, 0.84], [-0.4, 0.96, -0.47, 0.4, 0.85, 0.66], [0.3, 0.96, -0.47, 0.41, 0.93, 0.68],
  [1.15, 0.96, -0.47, 0.43, 0.87, 0.66], [1.85, 0.94, -0.46, 0.46, 0.64, 0.66], [2.24, 0.9, -0.44, 0.45, 0.51, 0.74],
  [2.38, 0.78, -0.33, 0.3, 0.38, 0.66],
];
const SIDE_GLASS = [-0.78, 1.72], TOP_GLASS = [-1.1, 1.85], PILLARS = [-0.4, 0.48, 1.3];
const HALF = 11; // points on each side of the ring, between the two centre points

function bodyShell(): { paint: THREE.BufferGeometry; glass: THREE.BufferGeometry } {
  // Smooth the stations into many thin slices.
  const cr = (a: number, b: number, c: number, d: number, t: number) => 0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - 3 * c + d - a) * t * t * t);
  const slices: number[][] = [], S = STATIONS, SUB = 8;
  for (let i = 0; i < S.length - 1; i++) for (let k = 0; k < SUB; k++) {
    const t = k / SUB, p = S[Math.max(0, i - 1)], q = S[i], r = S[i + 1], s = S[Math.min(S.length - 1, i + 2)];
    slices.push(q.map((_, j) => (j === 0 ? q[0] + (r[0] - q[0]) * t : cr(p[j], q[j], r[j], s[j], t))));
  }
  slices.push(S[S.length - 1]);
  // Each slice is a rounded ring: underside, sill, a slightly full flank, shoulder, window, roof edge, roof.
  const ring = ([z, w, yb, ys, yt, wt]: number[]) => {
    const cab = Math.max(0, yt - ys), mid = (yb + ys) / 2;
    const half = [[w * 0.55, yb], [w * 0.86, yb + 0.02], [w * 0.98, yb + 0.1], [w * 1.012, mid], [w * 1.0, ys - 0.09], [w * 0.972, ys - 0.015], [w * 0.94, ys + cab * 0.06],
      [(w * 0.94 + wt) / 2 + 0.01, ys + cab * 0.56], [wt + 0.015, yt - cab * 0.1 - 0.012], [wt * 0.88, yt - 0.012], [wt * 0.5, yt]];
    return [[0, yb], ...half, [0, yt], ...half.map(([x, y]) => [-x, y]).reverse()].map(([x, y]) => [x, y, z]);
  };
  const R = HALF * 2 + 2, pos: number[] = [], paint: number[] = [], glass: number[] = [];
  for (const s of slices) for (const p of ring(s)) pos.push(p[0], p[1], p[2]);
  for (let i = 0; i < slices.length - 1; i++) {
    const zMid = (slices[i][0] + slices[i + 1][0]) / 2, cabin = slices[i][4] - slices[i][3] > 0.14 && slices[i + 1][4] - slices[i + 1][3] > 0.14;
    const pillar = PILLARS.some((p) => Math.abs(zMid - p) < 0.065);
    for (let k = 0; k < R; k++) {
      const a = i * R + k, b = i * R + ((k + 1) % R), c = a + R, d = b + R;
      // Counting round the ring: 7 and 8 (and their mirror images) are the side windows; 9 to 14 are the top.
      const side = k === 7 || k === 8 || k === R - 9 || k === R - 8, top = k >= 9 && k <= R - 10;
      const isGlass = cabin && ((side && !pillar && zMid > SIDE_GLASS[0] && zMid < SIDE_GLASS[1]) || (top && zMid > TOP_GLASS[0] && zMid < TOP_GLASS[1]));
      (isGlass ? glass : paint).push(a, b, c, b, d, c);
    }
  }
  // Close the nose and tail.
  for (const i of [0, slices.length - 1]) for (let k = 1; k < R - 1; k++) paint.push(i * R, i * R + k, i * R + k + 1, i * R, i * R + k + 1, i * R + k);
  const make = (idx: number[]) => { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setIndex(idx); g.computeVertexNormals(); return g; };
  return { paint: make(paint), glass: make(glass) };
}

export class CarModel {
  readonly group = new THREE.Group();
  readonly lamp = new THREE.SpotLight(0xf4f8ff, 0, 110, 0.48, 0.55, 1.2);
  private readonly wheels: THREE.Group[] = [];
  private readonly head: THREE.MeshStandardMaterial; private readonly tail: THREE.MeshStandardMaterial;

  constructor() {
    const paint = new THREE.MeshPhysicalMaterial({ color: 0xb9bec6, metalness: 0.35, roughness: 0.36, clearcoat: 1, clearcoatRoughness: 0.05, envMapIntensity: 1.1 });
    const glass = new THREE.MeshPhysicalMaterial({ color: 0x0a1116, metalness: 0.2, roughness: 0.04, clearcoat: 1, envMapIntensity: 1.8 });
    const trim = new THREE.MeshStandardMaterial({ color: 0x141516, roughness: 0.75 });
    const chrome = new THREE.MeshStandardMaterial({ color: 0xd5d8dc, metalness: 1, roughness: 0.18 });
    const rubber = new THREE.MeshStandardMaterial({ color: 0x101011, roughness: 0.92 });
    const alloy = new THREE.MeshStandardMaterial({ color: 0xb9bdc2, metalness: 0.95, roughness: 0.3 });
    this.head = new THREE.MeshStandardMaterial({ color: 0xf2f6ff, emissive: 0xdfe9ff, emissiveIntensity: 0.25, roughness: 0.2 });
    this.tail = new THREE.MeshStandardMaterial({ color: 0x8a0f12, emissive: 0xff1a1a, emissiveIntensity: 0.35, roughness: 0.3 });
    const shell = bodyShell();
    const add = (g: THREE.BufferGeometry, m: THREE.Material, shadow = true) => { const me = new THREE.Mesh(g, m); me.castShadow = shadow; me.receiveShadow = true; this.group.add(me); return me; };
    add(shell.paint, paint); add(shell.glass, glass, false);
    const box = (w: number, h: number, d: number, x: number, y: number, z: number, rx = 0) => { const g = new THREE.BoxGeometry(w, h, d); g.rotateX(rx); g.translate(x, y, z); return g; };
    const R = CAR.wheelRadius, hw = CAR.track / 2, hb = CAR.wheelBase / 2, wy = CAR.attachY - CAR.suspRest;
    const trims: THREE.BufferGeometry[] = [box(1.25, 0.13, 0.06, 0, -0.3, -2.33), box(1.5, 0.2, 0.08, 0, -0.3, 2.34), // lower intake, rear valance
      box(0.05, 0.13, 2.3, -0.965, -0.43, 0), box(0.05, 0.13, 2.3, 0.965, -0.43, 0), // sills
      box(0.17, 0.09, 0.12, -1.03, 0.47, -0.78), box(0.17, 0.09, 0.12, 1.03, 0.47, -0.78)]; // mirrors
    for (const sx of [-1, 1]) for (const z of [-hb, hb]) { // dark wheel-arch lips
      const g = new THREE.RingGeometry(R + 0.02, R + 0.1, 24, 1, 0, Math.PI); g.rotateY((sx * Math.PI) / 2); g.translate(sx * 0.985, wy, z); trims.push(g.toNonIndexed());
    }
    add(mergeGeometries(trims.map((g) => (g.index ? g.toNonIndexed() : g))), trim);
    add(mergeGeometries([box(0.36, 0.05, 0.05, -0.62, 0.1, -2.3, 0.25), box(0.36, 0.05, 0.05, 0.62, 0.1, -2.3, 0.25)]), this.head, false); // slim headlights
    add(box(1.62, 0.045, 0.05, 0, 0.4, 2.3), this.tail, false); // one light bar across the tail
    add(mergeGeometries([box(0.46, 0.11, 0.02, 0, -0.12, -2.37), box(0.46, 0.11, 0.02, 0, 0.16, 2.37)]), new THREE.MeshStandardMaterial({ color: 0xf0f0ec, roughness: 0.6 }), false);
    void chrome;

    // Wheels: a rounded tyre, an alloy rim with five spokes and a dark brake disc behind.
    const tyre = new THREE.LatheGeometry([[R - 0.075, -0.125], [R - 0.02, -0.125], [R, -0.09], [R, 0.09], [R - 0.02, 0.125], [R - 0.075, 0.125]].map(([x, y]) => new THREE.Vector2(x, y)), 28);
    tyre.rotateZ(Math.PI / 2);
    const rimParts: THREE.BufferGeometry[] = [];
    const barrel = new THREE.CylinderGeometry(R - 0.075, R - 0.075, 0.2, 24, 1, true); barrel.rotateZ(Math.PI / 2); rimParts.push(barrel.toNonIndexed());
    const hub = new THREE.CylinderGeometry(0.07, 0.07, 0.05, 12); hub.rotateZ(Math.PI / 2); hub.translate(0.1, 0, 0); rimParts.push(hub.toNonIndexed());
    for (let s = 0; s < 5; s++) { const g = new THREE.BoxGeometry(0.03, R - 0.09, 0.085); g.translate(0.09, (R - 0.09) / 2, 0); g.rotateX((s * 2 * Math.PI) / 5); rimParts.push(g.toNonIndexed()); }
    const rim = mergeGeometries(rimParts), disc = new THREE.CylinderGeometry(R - 0.1, R - 0.1, 0.03, 20); disc.rotateZ(Math.PI / 2);
    for (let i = 0; i < 4; i++) {
      const spin = new THREE.Group(), pivot = new THREE.Group(), outer = i % 2 === 0 ? -1 : 1;
      for (const [g, m] of [[tyre, rubber], [rim, alloy], [disc, trim]] as const) { const me = new THREE.Mesh(g, m); me.castShadow = true; me.scale.x = outer; spin.add(me); }
      pivot.add(spin); this.wheels.push(pivot); this.group.add(pivot);
    }
    this.lamp.position.set(0, 0.2, -2.3); this.lamp.target.position.set(0, -0.5, -30);
    this.group.add(this.lamp, this.lamp.target);
  }

  update(car: Vehicle, pos: THREE.Vector3, quat: THREE.Quaternion, night: number, braking: boolean) {
    this.group.position.copy(pos); this.group.quaternion.copy(quat);
    car.wheels.forEach((w, i) => {
      const pivot = this.wheels[i];
      pivot.position.set(w.lx, CAR.attachY - w.length, w.lz);
      pivot.rotation.set(0, w.steer, 0);
      pivot.children[0].rotation.x = -w.spin;
    });
    this.lamp.intensity = night * 1500;
    this.head.emissiveIntensity = 0.25 + night * 4;
    this.tail.emissiveIntensity = (braking ? 3.5 : 0.35) + night * 0.8;
  }
}
