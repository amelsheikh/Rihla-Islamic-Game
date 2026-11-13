// The road surface, the car and its passengers, and the floating place names.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { CAR, type Vehicle } from '../physics/vehicle';
import type { World } from '../world/world';
import type { Named } from './buildings';
import { applySunShade, type SunShade } from './sunshade';
import { roadSurface } from './textures';


// ------------------------------------------------------------------ road
/** The road surface, plus a dusty verge on each side that fades into the ground. */
export function buildRoad(world: World, shade: SunShade): THREE.Group {
  const n = world.n, hw = world.halfWidth, group = new THREE.Group();
  const strip = (offs: number[], us: number[], alphas: number[] | null, yOf: (i: number, k: number, x: number, z: number) => number) => {
    const m = offs.length, pos = new Float32Array(n * m * 3), uv = new Float32Array(n * m * 2), col = new Float32Array(n * m * 4), idx: number[] = [];
    for (let i = 0; i < n; i++) {
      const a = Math.max(0, i - 1), b = Math.min(n - 1, i + 1);
      let dx = world.px[b] - world.px[a], dz = world.pz[b] - world.pz[a];
      const l = Math.hypot(dx, dz); dx /= l; dz /= l;
      for (let k = 0; k < m; k++) {
        const j = i * m + k, x = world.px[i] - dz * offs[k], z = world.pz[i] + dx * offs[k]; // right of travel is (-dz, dx)
        pos[j * 3] = x; pos[j * 3 + 1] = yOf(i, k, x, z); pos[j * 3 + 2] = z;
        uv[j * 2] = us[k]; uv[j * 2 + 1] = world.along[i] / 8;
        col.set([1, 1, 1, alphas ? alphas[k] : 1], j * 4);
      }
      if (i < n - 1) for (let k = 0; k < m - 1; k++) { const p = i * m + k, q = p + m; idx.push(p, p + 1, q, p + 1, q + 1, q); }
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); g.setAttribute('color', new THREE.BufferAttribute(col, 4));
    g.setIndex(idx); g.computeVertexNormals();
    return g;
  };
  const tex = roadSurface();
  const surface = new THREE.MeshStandardMaterial({ map: tex.map, normalMap: tex.normalMap, roughness: 0.92 });
  applySunShade(surface, shade);
  const road = new THREE.Mesh(strip([-(hw + 0.5), -hw, hw, hw + 0.5], [0, 0.06, 0.94, 1], null, (i, k) => world.py[i] - (k === 0 || k === 3 ? 0.12 : 0)), surface);
  const vergeMat = new THREE.MeshStandardMaterial({ map: tex.map, roughness: 0.95, vertexColors: true, transparent: true, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -3, polygonOffsetUnits: -3 });
  applySunShade(vergeMat, shade);
  for (const side of [-1, 1]) {
    const verge = new THREE.Mesh(strip([side * (hw + 0.3), side * (hw + 1.6), side * (hw + 4.2)].sort((a, b) => a - b), side < 0 ? [0.02, 0.05, 0.08] : [0.92, 0.95, 0.98], side < 0 ? [0, 0.75, 0.9] : [0.9, 0.75, 0],
      (i, k, x, z) => world.groundY(x, z) + 0.05), vergeMat);
    verge.receiveShadow = true; verge.matrixAutoUpdate = false; verge.renderOrder = 1; group.add(verge);
  }
  road.receiveShadow = true; road.matrixAutoUpdate = false;
  group.add(road);
  return group;
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
    // Jiddo Ibrahim at the wheel in his tarbush, and the young traveller beside him. No faces are drawn.
    const skin = new THREE.MeshStandardMaterial({ color: 0xc69a74, roughness: 0.8 });
    const robe = new THREE.MeshStandardMaterial({ color: 0x5b6470, roughness: 0.9 }), tunic = new THREE.MeshStandardMaterial({ color: 0xe8e2d2, roughness: 0.9 });
    const fez = new THREE.MeshStandardMaterial({ color: 0x9c1f24, roughness: 0.7 }), cap = new THREE.MeshStandardMaterial({ color: 0xf2efe6, roughness: 0.9 });
    p.add(new THREE.CapsuleGeometry(0.2, 0.42, 3, 8), robe, 0.32, 0.72, 0.1); p.add(new THREE.SphereGeometry(0.135, 10, 8), skin, 0.32, 1.2, 0.08);
    p.add(new THREE.CylinderGeometry(0.105, 0.125, 0.17, 10), fez, 0.32, 1.37, 0.08);
    p.add(new THREE.CapsuleGeometry(0.16, 0.26, 3, 8), tunic, -0.34, 0.62, 0.1); p.add(new THREE.SphereGeometry(0.12, 10, 8), skin, -0.34, 0.98, 0.08);
    p.add(new THREE.SphereGeometry(0.125, 10, 6, 0, 6.3, 0, 1.3), cap, -0.34, 1.0, 0.08);
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

// ------------------------------------------------------------------ place names
export class Labels {
  readonly group = new THREE.Group();
  constructor(named: Named[]) {
    for (const nm of named) {
      const cv = document.createElement('canvas'); cv.width = 512; cv.height = 160;
      const g = cv.getContext('2d')!;
      g.fillStyle = 'rgba(30,24,16,0.72)'; g.beginPath(); g.roundRect(4, 4, 504, 152, 22); g.fill();
      g.strokeStyle = '#c9a54a'; g.lineWidth = 4; g.stroke();
      g.fillStyle = '#f4e9cf'; g.textAlign = 'center';
      g.font = '52px Amiri, "Noto Naskh Arabic", serif'; g.direction = 'rtl'; g.fillText(nm.ar, 256, 66, 480);
      g.font = '36px Georgia, serif'; g.direction = 'ltr'; g.fillText(nm.en, 256, 124, 480);
      const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.SRGBColorSpace;
      const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthWrite: false, fog: false, sizeAttenuation: false, transparent: true }));
      sp.scale.set(0.2 * nm.size, 0.0625 * nm.size, 1); sp.position.set(nm.x, nm.y, nm.z);
      sp.userData.far = nm.size < 0.7 ? 900 : 3200;
      this.group.add(sp);
    }
  }
  /** Names keep a fixed size on screen; the very near and the very far are hidden. */
  update(cam: THREE.Vector3) {
    if (!this.group.visible) return;
    for (const sp of this.group.children as THREE.Sprite[]) {
      const d = sp.position.distanceTo(cam), far = sp.userData.far as number;
      sp.material.opacity = Math.min(1, Math.max(0, (d - 70) / 60)) * Math.min(1, Math.max(0, (far - d) / (far * 0.15)));
      sp.visible = sp.material.opacity > 0.02;
    }
  }
}
