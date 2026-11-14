// The road with its markings and guard rails, and the floating place names.
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import type { World } from '../world/world';
import type { Named } from './buildings';
import type { Photos } from './photo';
import { applySunShade, type SunShade } from './sunshade';

// ------------------------------------------------------------------ road
/** A modern two-lane road: asphalt, painted lines, gravel verges and guard rails above the drops. */
export function buildRoad(world: World, shade: SunShade, photos: Photos, rapier: RAPIER.World | null): THREE.Group {
  const n = world.n, hw = world.halfWidth, group = new THREE.Group();
  const dirAt = (i: number) => { const a = Math.max(0, i - 1), b = Math.min(n - 1, i + 1), dx = world.px[b] - world.px[a], dz = world.pz[b] - world.pz[a], l = Math.hypot(dx, dz); return [dx / l, dz / l]; };
  /** A ribbon following the road; `from`/`to` limit it to a stretch, for dashes. */
  const strip = (offs: number[], us: number[], alphas: number[] | null, yOf: (i: number, k: number, x: number, z: number) => number, vScale: number, from = 0, to = n - 1, pos: number[] = [], uv: number[] = [], col: number[] = [], idx: number[] = []) => {
    const m = offs.length, base = pos.length / 3;
    for (let i = from; i <= to; i++) {
      const [dx, dz] = dirAt(i);
      for (let k = 0; k < m; k++) {
        const x = world.px[i] - dz * offs[k], z = world.pz[i] + dx * offs[k]; // right of travel is (-dz, dx)
        pos.push(x, yOf(i, k, x, z), z); uv.push(us[k], world.along[i] / vScale); col.push(1, 1, 1, alphas ? alphas[k] : 1);
      }
      if (i < to) for (let k = 0; k < m - 1; k++) { const p = base + (i - from) * m + k, q = p + m; idx.push(p, p + 1, q, p + 1, q + 1, q); }
    }
    return { pos, uv, col, idx };
  };
  const mesh = (d: { pos: number[]; uv: number[]; col: number[]; idx: number[] }, mat: THREE.Material) => {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(d.pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(d.uv, 2)); g.setAttribute('color', new THREE.Float32BufferAttribute(d.col, 4));
    g.setIndex(d.idx); g.computeVertexNormals();
    const me = new THREE.Mesh(g, mat); me.receiveShadow = true; me.matrixAutoUpdate = false; group.add(me);
    return me;
  };
  const asphalt = new THREE.MeshStandardMaterial({ map: photos.asphalt.color, normalMap: photos.asphalt.normal, roughness: 0.85, color: 0xd8d8d8 });
  applySunShade(asphalt, shade);
  mesh(strip([-(hw + 0.4), -hw, hw, hw + 0.4], [0, 0.1, 1.85, 1.95], null, (i, k) => world.py[i] - (k === 0 || k === 3 ? 0.14 : 0), 4), asphalt);
  // Painted lines sit a hair above the asphalt: solid at the edges, dashed down the middle.
  const paint = new THREE.MeshStandardMaterial({ color: 0xe9e6dc, roughness: 0.7, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2 });
  applySunShade(paint, shade);
  const lines = strip([-(hw - 0.35), -(hw - 0.5)], [0, 1], null, (i) => world.py[i] + 0.015, 1);
  strip([hw - 0.5, hw - 0.35], [0, 1], null, (i) => world.py[i] + 0.015, 1, 0, n - 1, lines.pos, lines.uv, lines.col, lines.idx);
  for (let i = 2; i < n - 3; i += 4) strip([-0.07, 0.07], [0, 1], null, (j) => world.py[j] + 0.015, 1, i, i + 1, lines.pos, lines.uv, lines.col, lines.idx);
  mesh(lines, paint);
  const vergeMat = new THREE.MeshStandardMaterial({ map: photos.soil.color, color: 0xb9aa8e, roughness: 0.95, vertexColors: true, transparent: true, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -3, polygonOffsetUnits: -3 });
  applySunShade(vergeMat, shade);
  for (const side of [-1, 1]) {
    const offs = [side * (hw + 0.3), side * (hw + 1.8), side * (hw + 4.5)].sort((a, b) => a - b);
    mesh(strip(offs, offs.map((o) => o / 4), side < 0 ? [0, 0.8, 0.95] : [0.95, 0.8, 0], (i, k, x, z) => world.groundY(x, z) + 0.05, 4), vergeMat).renderOrder = 1;
  }
  // Guard rails wherever the ground falls away beside the road.
  const steel = new THREE.MeshStandardMaterial({ color: 0xaab0b4, roughness: 0.45, metalness: 0.85, side: THREE.DoubleSide });
  applySunShade(steel, shade);
  const rail = { pos: [] as number[], uv: [] as number[], col: [] as number[], idx: [] as number[] }, posts: THREE.Matrix4[] = [];
  for (const side of [-1, 1]) {
    const need = new Uint8Array(n);
    for (let i = 0; i < n; i++) { const [dx, dz] = dirAt(i), o = side * (hw + 8); need[i] = world.py[i] - world.terrainY(world.px[i] - dz * o, world.pz[i] + dx * o) > 2.4 ? 1 : 0; }
    const wide = new Uint8Array(n);
    for (let i = 0; i < n; i++) for (let k = -3; k <= 3; k++) if (need[Math.min(n - 1, Math.max(0, i + k))]) wide[i] = 1;
    for (let i = 4; i < n - 4; i++) {
      if (!wide[i]) continue;
      let j = i; while (j < n - 5 && wide[j + 1]) j++;
      if (j - i >= 3) {
        const o = side * (hw + 0.75);
        strip([o, o], [0, 1], null, (q, k) => world.py[q] + (k ? 0.78 : 0.48), 2, i, j, rail.pos, rail.uv, rail.col, rail.idx);
        for (let q = i; q <= j; q++) {
          const [dx, dz] = dirAt(q), x = world.px[q] - dz * o, z = world.pz[q] + dx * o;
          if (q % 2 === 0) posts.push(new THREE.Matrix4().compose(new THREE.Vector3(x, world.py[q] + 0.3, z), new THREE.Quaternion(), new THREE.Vector3(0.09, 0.9, 0.09)));
          if (rapier && q % 2 === 0 && q < j) {
            const yaw = Math.atan2(-dz, dx);
            rapier.createCollider(RAPIER.ColliderDesc.cuboid(3.2, 0.6, 0.12).setTranslation(x, world.py[q] + 0.5, z).setRotation({ x: 0, y: Math.sin(yaw / 2), z: 0, w: Math.cos(yaw / 2) }));
          }
        }
      }
      i = j;
    }
  }
  if (rail.idx.length) {
    mesh(rail, steel).castShadow = true;
    const im = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1), steel, posts.length);
    posts.forEach((m, i) => im.setMatrixAt(i, m)); im.computeBoundingSphere(); im.matrixAutoUpdate = false; group.add(im);
  }
  return group;
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
