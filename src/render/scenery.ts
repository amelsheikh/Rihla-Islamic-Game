// Grey-box scenery: the road surface, the car, block-out landmarks, placeholder trees and place labels.
// Everything here is a stand-in with the right position and rough size; Phase 1b replaces the looks.
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { CAR, type Vehicle } from '../physics/vehicle';
import type { World } from '../world/world';

function rng(seed: number) { return () => ((seed = (seed * 16807) % 2147483647) / 2147483647); }

// ------------------------------------------------------------------ road
export function buildRoad(world: World): THREE.Mesh {
  const n = world.n, hw = world.halfWidth, sh = world.shoulder, offs = [-(hw + sh), -hw, hw, hw + sh], drop = [0.4, 0, 0, 0.4];
  const pos = new Float32Array(n * 4 * 3), uv = new Float32Array(n * 4 * 2), idx: number[] = [];
  for (let i = 0; i < n; i++) {
    const a = Math.max(0, i - 1), b = Math.min(n - 1, i + 1);
    let dx = world.px[b] - world.px[a], dz = world.pz[b] - world.pz[a];
    const l = Math.hypot(dx, dz); dx /= l; dz /= l;
    for (let k = 0; k < 4; k++) {
      const j = i * 4 + k;
      // Right of the direction of travel is (-dz, dx).
      pos[j * 3] = world.px[i] - dz * offs[k]; pos[j * 3 + 1] = world.py[i] - drop[k]; pos[j * 3 + 2] = world.pz[i] + dx * offs[k];
      uv[j * 2] = [0, 0.12, 0.88, 1][k]; uv[j * 2 + 1] = world.along[i] / 9;
    }
    if (i < n - 1) for (let k = 0; k < 3; k++) { const p = i * 4 + k, q = p + 4; idx.push(p, p + 1, q, p + 1, q + 1, q); }
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
  g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
  g.setIndex(idx);
  g.computeVertexNormals();
  // A packed-stone road: pale, with two lighter wheel tracks and darker verges.
  const cv = document.createElement('canvas'); cv.width = 128; cv.height = 256;
  const c = cv.getContext('2d')!, r = rng(11);
  for (let y = 0; y < 256; y += 2) for (let x = 0; x < 128; x += 2) {
    const u = x / 128, verge = Math.min(1, Math.min(u, 1 - u) / 0.12);
    const track = Math.max(0, 1 - Math.abs(Math.abs(u - 0.5) - 0.13) / 0.06);
    const v = 96 + verge * 26 + track * 16 + (r() - 0.5) * 20;
    c.fillStyle = `rgb(${v + 14},${v + 2},${v - 22})`; c.fillRect(x, y, 2, 2);
  }
  const map = new THREE.CanvasTexture(cv);
  map.wrapT = THREE.RepeatWrapping; map.colorSpace = THREE.SRGBColorSpace; map.anisotropy = 8;
  const mesh = new THREE.Mesh(g, new THREE.MeshStandardMaterial({ map, roughness: 0.9 }));
  mesh.receiveShadow = true; mesh.matrixAutoUpdate = false;
  return mesh;
}

// ------------------------------------------------------------------ car
class Parts {
  private byMat = new Map<THREE.Material, THREE.BufferGeometry[]>();
  add(geo: THREE.BufferGeometry, mat: THREE.Material, x = 0, y = 0, z = 0, rx = 0, ry = 0, rz = 0) {
    geo.applyMatrix4(new THREE.Matrix4().compose(new THREE.Vector3(x, y, z), new THREE.Quaternion().setFromEuler(new THREE.Euler(rx, ry, rz)), new THREE.Vector3(1, 1, 1)));
    let list = this.byMat.get(mat);
    if (!list) this.byMat.set(mat, (list = []));
    list.push(geo.index ? geo.toNonIndexed() : geo);
  }
  build(): THREE.Group {
    const g = new THREE.Group();
    for (const [mat, list] of this.byMat) { const m = new THREE.Mesh(mergeGeometries(list), mat); m.castShadow = true; m.receiveShadow = true; g.add(m); }
    return g;
  }
}

export class CarModel {
  readonly group = new THREE.Group();
  readonly lamp = new THREE.SpotLight(0xffe2a8, 0, 70, 0.5, 0.6, 1.4);
  private readonly wheels: THREE.Group[] = [];
  private readonly lampMat: THREE.MeshStandardMaterial;

  constructor() {
    const paint = new THREE.MeshStandardMaterial({ color: 0x6b1d1d, roughness: 0.35, metalness: 0.1 });
    const dark = new THREE.MeshStandardMaterial({ color: 0x1c1a18, roughness: 0.7 });
    const brass = new THREE.MeshStandardMaterial({ color: 0xb8923c, roughness: 0.3, metalness: 0.9 });
    const canvas = new THREE.MeshStandardMaterial({ color: 0xd6cdb4, roughness: 0.95 });
    const leather = new THREE.MeshStandardMaterial({ color: 0x4a2c1a, roughness: 0.8 });
    const wood = new THREE.MeshStandardMaterial({ color: 0xb08a55, roughness: 0.7 });
    this.lampMat = new THREE.MeshStandardMaterial({ color: 0xfff0c0, emissive: 0xffd890, emissiveIntensity: 0 });
    const box = (w: number, h: number, d: number) => new THREE.BoxGeometry(w, h, d);
    const p = new Parts();
    p.add(box(1.2, 0.12, 3.9), dark, 0, -0.1, 0); // frame
    p.add(box(0.82, 0.5, 1.15), paint, 0, 0.3, -1.2); // bonnet
    p.add(box(0.86, 0.66, 0.1), brass, 0, 0.3, -1.82); // radiator
    p.add(box(1.4, 0.12, 2.25), paint, 0, 0.02, 0.5); // floor
    p.add(box(0.07, 0.5, 2.25), paint, -0.7, 0.3, 0.5); p.add(box(0.07, 0.5, 2.25), paint, 0.7, 0.3, 0.5); // sides
    p.add(box(1.4, 0.55, 0.07), paint, 0, 0.32, 1.62); p.add(box(1.4, 0.5, 0.07), paint, 0, 0.3, -0.62); // back, scuttle
    p.add(box(1.25, 0.14, 0.5), leather, 0, 0.3, 0.05); p.add(box(1.25, 0.5, 0.12), leather, 0, 0.55, 0.32); // front bench
    p.add(box(1.25, 0.14, 0.5), leather, 0, 0.3, 1.1); p.add(box(1.25, 0.5, 0.12), leather, 0, 0.55, 1.4); // rear bench
    p.add(box(1.5, 0.05, 2.5), canvas, 0, 1.52, 0.45); // roof
    for (const [x, z] of [[-0.7, -0.6], [0.7, -0.6], [-0.7, 1.6], [0.7, 1.6]]) p.add(new THREE.CylinderGeometry(0.02, 0.02, 1.0, 6), dark, x, 1.02, z);
    p.add(box(1.34, 0.5, 0.03), new THREE.MeshStandardMaterial({ color: 0xcfe4ee, roughness: 0.05, transparent: true, opacity: 0.3 }), 0, 0.82, -0.62); // windscreen
    for (const x of [-0.71, 0.71]) {
      p.add(box(0.26, 0.04, 1.05), dark, x, 0.42, -1.45); p.add(box(0.26, 0.04, 1.05), dark, x, 0.42, 1.45); // mudguards
      p.add(box(0.26, 0.04, 1.8), dark, x, -0.12, 0); // running board
      p.add(new THREE.CylinderGeometry(0.11, 0.09, 0.16, 12), brass, x * 0.62, 0.42, -1.95, Math.PI / 2); // headlamps
      p.add(new THREE.CircleGeometry(0.1, 12), this.lampMat, x * 0.62, 0.42, -2.035, 0, Math.PI);
    }
    p.add(new THREE.TorusGeometry(CAR.wheelRadius - 0.06, 0.06, 6, 18), dark, 0, 0.35, 1.78); // spare wheel
    p.add(new THREE.TorusGeometry(0.19, 0.02, 6, 16), wood, 0.32, 0.78, -0.28, -0.9); // steering wheel
    this.group.add(p.build());

    for (let i = 0; i < 4; i++) {
      const w = new Parts(), R = CAR.wheelRadius;
      w.add(new THREE.TorusGeometry(R - 0.055, 0.055, 8, 24), dark, 0, 0, 0, 0, Math.PI / 2);
      w.add(new THREE.CylinderGeometry(0.09, 0.09, 0.16, 10), brass, 0, 0, 0, 0, 0, Math.PI / 2);
      for (let s = 0; s < 10; s++) w.add(new THREE.CylinderGeometry(0.017, 0.022, R - 0.08, 5), wood, 0, Math.cos((s * Math.PI) / 5) * (R / 2 - 0.02), Math.sin((s * Math.PI) / 5) * (R / 2 - 0.02), (s * Math.PI) / 5);
      const spin = w.build(), pivot = new THREE.Group();
      pivot.add(spin); this.wheels.push(pivot); this.group.add(pivot);
    }
    this.lamp.position.set(0, 0.5, -2); this.lamp.target.position.set(0, -0.6, -22);
    this.group.add(this.lamp, this.lamp.target);
  }

  update(car: Vehicle, pos: THREE.Vector3, quat: THREE.Quaternion, night: number) {
    this.group.position.copy(pos); this.group.quaternion.copy(quat);
    car.wheels.forEach((w, i) => {
      const pivot = this.wheels[i];
      pivot.position.set(w.lx, CAR.attachY - w.length, w.lz);
      pivot.rotation.set(0, w.steer, 0);
      pivot.children[0].rotation.x = -w.spin;
    });
    this.lamp.intensity = night * 900; this.lampMat.emissiveIntensity = night * 3;
  }
}

// ------------------------------------------------------------------ block-out buildings
const STONE = 0xcfc3a4, WHITE = 0xe9e4d6, LEAD = 0x565b62, TILE = 0x4d7d98, GREEN = 0x4f6f3a, TRUNK = 0x6b5a45, OLIVE = 0x66754d;

export class Blockout {
  readonly group = new THREE.Group();
  readonly labels = new THREE.Group();
  private boxes: { m: THREE.Matrix4; c: number }[] = [];
  private domes: { m: THREE.Matrix4; c: number }[] = [];
  private cyls: { m: THREE.Matrix4; c: number }[] = [];
  private cones: { m: THREE.Matrix4; c: number }[] = [];
  private blobs: { m: THREE.Matrix4; c: number }[] = [];

  constructor(private world: World, private rapier: RAPIER.World | null) {}

  private mat(x: number, y: number, z: number, sx: number, sy: number, sz: number, yaw = 0) {
    return new THREE.Matrix4().compose(new THREE.Vector3(x, y, z), new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), yaw), new THREE.Vector3(sx, sy, sz));
  }
  /** A box standing on the terrain; `y` is the height of its base. */
  box(x: number, y: number, z: number, sx: number, sy: number, sz: number, yaw = 0, c = STONE, solid = true) {
    this.boxes.push({ m: this.mat(x, y + sy / 2, z, sx, sy, sz, yaw), c });
    if (solid && this.rapier) this.rapier.createCollider(RAPIER.ColliderDesc.cuboid(sx / 2, sy / 2 + 2, sz / 2).setTranslation(x, y + sy / 2 - 2, z).setRotation({ x: 0, y: Math.sin(yaw / 2), z: 0, w: Math.cos(yaw / 2) }));
  }
  dome(x: number, y: number, z: number, r: number, c = WHITE, tall = 1) { this.domes.push({ m: this.mat(x, y, z, r, r * tall, r), c }); }
  cyl(x: number, y: number, z: number, r: number, h: number, c = STONE) { this.cyls.push({ m: this.mat(x, y + h / 2, z, r, h, r), c }); }
  palm(x: number, z: number, h: number) {
    const y = this.world.terrainY(x, z);
    this.cyls.push({ m: this.mat(x, y + h / 2, z, 0.22, h, 0.22), c: TRUNK });
    this.cones.push({ m: this.mat(x, y + h + 0.3, z, 2.6, 1.6, 2.6), c: GREEN });
  }
  tree(x: number, z: number, s: number) {
    const y = this.world.terrainY(x, z);
    this.blobs.push({ m: this.mat(x, y + s * 0.7, z, s * 1.2, s, s * 1.2), c: OLIVE });
  }
  private clearOfRoad(x: number, z: number, margin: number) { const h = this.world.nearestRoad(x, z); return !h || h.dist > this.world.halfWidth + margin; }

  label(x: number, y: number, z: number, en: string, ar: string, size = 1) {
    const cv = document.createElement('canvas'); cv.width = 512; cv.height = 160;
    const g = cv.getContext('2d')!;
    g.fillStyle = 'rgba(30,24,16,0.72)'; g.beginPath(); g.roundRect(4, 4, 504, 152, 22); g.fill();
    g.strokeStyle = '#c9a54a'; g.lineWidth = 4; g.stroke();
    g.fillStyle = '#f4e9cf'; g.textAlign = 'center';
    g.font = '52px Amiri, "Noto Naskh Arabic", serif'; g.direction = 'rtl'; g.fillText(ar, 256, 66);
    g.font = '38px Georgia, serif'; g.direction = 'ltr'; g.fillText(en, 256, 124);
    const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.SRGBColorSpace;
    const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthWrite: false, fog: false, sizeAttenuation: false, transparent: true }));
    sp.scale.set(0.2 * size, 0.0625 * size, 1); sp.position.set(x, y, z);
    this.labels.add(sp);
  }

  /** Labels keep a fixed size on screen; hide the ones that are very close or very far. */
  updateLabels(cam: THREE.Vector3) {
    if (!this.labels.visible) return;
    for (const sp of this.labels.children as THREE.Sprite[]) {
      const d = sp.position.distanceTo(cam);
      sp.material.opacity = Math.min(1, Math.max(0, (d - 70) / 60)) * Math.min(1, Math.max(0, (3200 - d) / 500));
      sp.visible = sp.material.opacity > 0.02;
    }
  }

  /** Lay out every block-out in the corridor. */
  populate() {
    const w = this.world, P = (id: string) => w.data.places.find((p) => p.id === id)!, r = rng(42);
    const ty = (x: number, z: number) => w.terrainY(x, z);

    // Al-Quds: the city wall as a ring of boxes following the ground, with towers at the corners.
    const wall = w.data.walls;
    for (let i = 0; i < wall.length; i++) {
      const [ax, az] = wall[i], [bx, bz] = wall[(i + 1) % wall.length], len = Math.hypot(bx - ax, bz - az), steps = Math.ceil(len / 11), yaw = Math.atan2(-(bz - az), bx - ax);
      for (let s = 0; s < steps; s++) {
        const x = ax + ((bx - ax) * (s + 0.5)) / steps, z = az + ((bz - az) * (s + 0.5)) / steps;
        const base = Math.min(ty(x, z), ty(ax + ((bx - ax) * s) / steps, az + ((bz - az) * s) / steps)) - 1;
        this.box(x, base, z, len / steps + 0.4, 10, 2.4, yaw);
      }
      this.box(ax, ty(ax, az) - 1, az, 6, 14, 6, yaw);
    }
    const inside = (x: number, z: number) => {
      let c = false;
      for (let i = 0, j = wall.length - 1; i < wall.length; j = i++) {
        const [xi, zi] = wall[i], [xj, zj] = wall[j];
        if (zi > z !== zj > z && x < ((xj - xi) * (z - zi)) / (zj - zi) + xi) c = !c;
      }
      return c;
    };
    const dome = P('dome-of-the-rock'), qibli = P('al-qibli');
    // The Dome of the Rock: octagon, drum, and the dark lead-covered dome it had in 1910.
    {
      const y = ty(dome.x, dome.z);
      this.cyls.push({ m: this.mat(dome.x, y + 2.5, dome.z, 19, 5, 19, Math.PI / 8), c: WHITE });
      this.cyls.push({ m: this.mat(dome.x, y + 8, dome.z, 19, 6, 19, Math.PI / 8), c: TILE });
      this.cyl(dome.x, y + 11, dome.z, 10.5, 6, TILE);
      this.dome(dome.x, y + 17, dome.z, 10.8, LEAD, 1.08);
      this.cyl(dome.x, y + 28, dome.z, 0.25, 3.5, 0xb8923c);
      if (this.rapier) this.rapier.createCollider(RAPIER.ColliderDesc.cylinder(8, 19).setTranslation(dome.x, y + 6, dome.z));
    }
    {
      const y = ty(qibli.x, qibli.z);
      this.box(qibli.x, y, qibli.z, 30, 9, 42, 0, WHITE);
      this.dome(qibli.x, y + 9, qibli.z + 14, 6, LEAD);
    }
    const [minX, maxX, minZ, maxZ] = [Math.min(...wall.map((p) => p[0])), Math.max(...wall.map((p) => p[0])), Math.min(...wall.map((p) => p[1])), Math.max(...wall.map((p) => p[1]))];
    for (let i = 0; i < 900; i++) {
      const x = minX + r() * (maxX - minX), z = minZ + r() * (maxZ - minZ);
      if (!inside(x, z) || Math.hypot(x - dome.x, z - (dome.z + qibli.z) / 2) < 95 || Math.hypot(x - dome.x, z - dome.z) < 80) continue;
      const s = 7 + r() * 8, h = 5 + r() * 6;
      this.box(x, ty(x, z) - 1, z, s, h, 6 + r() * 8, r() * 0.5, r() > 0.5 ? STONE : WHITE, false);
      if (r() > 0.55) this.dome(x, ty(x, z) + h - 1, z, s * 0.3, WHITE);
    }
    // Villages and khans beside the road.
    const hamlet = (id: string, count: number, radius: number, margin: number) => {
      const p = P(id);
      for (let i = 0, placed = 0; i < count * 6 && placed < count; i++) {
        const a = r() * 7, d = Math.sqrt(r()) * radius, x = p.x + Math.cos(a) * d, z = p.z + Math.sin(a) * d;
        if (!this.clearOfRoad(x, z, margin)) continue;
        const s = 6 + r() * 5, h = 3.5 + r() * 2.5;
        this.box(x, ty(x, z) - 1.5, z, s, h + 1.5, 6 + r() * 4, r() * 3, r() > 0.4 ? STONE : WHITE);
        if (r() > 0.7) this.dome(x, ty(x, z) + h, z, s * 0.28, WHITE);
        placed++;
      }
    };
    hamlet('al-eizariya', 26, 85, 6);
    hamlet('ariha', 70, 240, 6);
    const khan = (id: string) => {
      const p = P(id), y = ty(p.x, p.z) - 1.5, s = 30;
      for (const [dx, dz, sx, sz] of [[0, -s / 2, s, 3], [0, s / 2, s, 3], [-s / 2, 0, 3, s], [s / 2, 0, 3, s]]) this.box(p.x + dx, y, p.z + dz, sx, 7.5, sz, 0, STONE);
    };
    khan('khan-al-ahmar'); khan('khan-al-hatruri');
    {
      // Maqam al-Nabi Musa: a walled compound with many small white domes and a minaret.
      const p = P('nabi-musa'), y = ty(p.x, p.z) - 1.5, s = 46;
      for (const [dx, dz, sx, sz] of [[0, -s / 2, s, 4], [0, s / 2, s, 4], [-s / 2, 0, 4, s], [s / 2, 0, 4, s]]) this.box(p.x + dx, y, p.z + dz, sx, 8, sz, 0, WHITE);
      for (let i = 0; i < 9; i++) this.dome(p.x - 16 + (i % 3) * 16, y + 8, p.z - 16 + Math.floor(i / 3) * 16, 4.2, WHITE);
      this.cyl(p.x + 20, y, p.z - 20, 1.6, 17, WHITE); this.dome(p.x + 20, y + 17, p.z - 20, 1.7, WHITE, 1.4);
    }
    // Date palms of the Ariha oasis and scattered trees on the western hills.
    const ar = P('ariha');
    for (let i = 0; i < 900; i++) {
      const a = r() * 7, d = Math.sqrt(r()) * 480, x = ar.x + Math.cos(a) * d, z = ar.z + Math.sin(a) * d;
      if (this.clearOfRoad(x, z, 3)) this.palm(x, z, 6 + r() * 5);
    }
    const ni = w.near.info;
    for (let i = 0; i < 14000 && this.blobs.length < 3200; i++) {
      const x = ni.x0 + 200 + r() * 2300, z = ni.z0 + 200 + r() * (ni.rows * ni.cell - 400);
      if (x > 500 + r() * 900 || inside(x, z) || !this.clearOfRoad(x, z, 3)) continue;
      if (Math.abs(ty(x + 3, z) - ty(x - 3, z)) + Math.abs(ty(x, z + 3) - ty(x, z - 3)) > 3.5) continue;
      this.tree(x, z, 1.2 + r() * 1.1);
    }
    // The sea-level marker stone beside the road.
    {
      const s = w.roadAt(w.data.road.seaLevelAt), x = s.x - s.dz * (w.halfWidth + 2.5), z = s.z + s.dx * (w.halfWidth + 2.5);
      this.box(x, ty(x, z) - 0.5, z, 1.1, 3, 0.5, Math.atan2(-s.dz, s.dx), WHITE);
      this.label(x, ty(x, z) + 7, z, 'Sea level · 0 m', 'مستوى سطح البحر', 0.8);
    }
    for (const p of w.data.places) this.label(p.x, p.y + (p.kind === 'city' || p.kind === 'landmark' ? 46 : 26), p.z, p.name.en, p.name.ar, p.kind === 'city' ? 1.2 : 0.9);
    this.finish();
  }

  private finish() {
    const mat = new THREE.MeshStandardMaterial({ roughness: 0.9 });
    const make = (geo: THREE.BufferGeometry, list: { m: THREE.Matrix4; c: number }[], shadow: boolean) => {
      if (!list.length) return;
      const im = new THREE.InstancedMesh(geo, mat, list.length), col = new THREE.Color();
      list.forEach((it, i) => { im.setMatrixAt(i, it.m); im.setColorAt(i, col.setHex(it.c)); });
      im.castShadow = shadow; im.receiveShadow = true; im.computeBoundingSphere();
      this.group.add(im);
    };
    make(new THREE.BoxGeometry(1, 1, 1), this.boxes, true);
    make(new THREE.SphereGeometry(1, 14, 6, 0, Math.PI * 2, 0, Math.PI / 2), this.domes, true);
    make(new THREE.CylinderGeometry(1, 1, 1, 8), this.cyls, true);
    make(new THREE.ConeGeometry(1, 1, 7), this.cones, true);
    make(new THREE.IcosahedronGeometry(1, 0), this.blobs, false);
  }
}
