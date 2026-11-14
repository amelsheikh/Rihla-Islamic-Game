// Terrain rendering: the near terrain as chunks with levels of detail, the far terrain as one mesh out to
// the horizon, and the colour and normal textures baked from the heights when the game loads.
import * as THREE from 'three';
import type { Heightmap } from '../world/heightmap';
import { V_SCALE } from '../world/warp';
import type { World } from '../world/world';
import { applySunShade, SunShade } from './sunshade';
import type { Photos } from './photo';
import type { Plant } from './vegetation';

const CHUNK = 128; // cells per chunk side
const STRIDES = [1, 2, 4, 8, 16];

function hash2(x: number, y: number) { const s = Math.sin(x * 127.1 + y * 311.7) * 43758.5453; return s - Math.floor(s); }
function vnoise(x: number, y: number) {
  const ix = Math.floor(x), iy = Math.floor(y), fx = x - ix, fy = y - iy;
  const u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
  return (hash2(ix, iy) * (1 - u) + hash2(ix + 1, iy) * u) * (1 - v) + (hash2(ix, iy + 1) * (1 - u) + hash2(ix + 1, iy + 1) * u) * v;
}
const fbm = (x: number, y: number) => vnoise(x, y) * 0.55 + vnoise(x * 2.1, y * 2.1) * 0.3 + vnoise(x * 4.3, y * 4.3) * 0.15;
const sstep = (a: number, b: number, x: number) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
const mix3 = (a: number[], b: number[], t: number) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];

// Grey-box palette (sRGB 0..255). Phase 1b replaces this with proper materials.
const C = {
  limestone: [168, 156, 122], scrub: [98, 112, 62], terra: [138, 96, 66],
  desert: [194, 160, 112], desertRock: [136, 106, 74], strata: [214, 186, 136],
  marl: [204, 192, 158], oasis: [70, 104, 48], salt: [232, 230, 220], seabed: [70, 100, 110],
};

/** Colour and object-space normal textures for a height grid, one texel per `step` cells. */
function bake(hm: Heightmap, world: World, texW: number, texH: number, plants: Plant[]) {
  const { x0, z0, cell, cols, rows } = hm.info, W = cols * cell, D = rows * cell;
  const albedo = new Uint8Array(texW * texH * 4), normal = new Uint8Array(texW * texH * 4);
  const ariha = world.data.places.find((p) => p.id === 'ariha')!, waterY = world.data.waterY;
  const ex = W / (texW - 1), ez = D / (texH - 1);
  for (let r = 0; r < texH; r++) for (let c = 0; c < texW; c++) {
    const x = x0 + c * ex, z = z0 + r * ez, h = hm.at(x, z);
    const dx = (hm.at(x + ex, z) - hm.at(x - ex, z)) / (2 * ex), dz = (hm.at(x, z + ez) - hm.at(x, z - ez)) / (2 * ez);
    const slope = Math.hypot(dx, dz), elev = h * V_SCALE;
    const n1 = fbm(x / 90, z / 90), n2 = fbm(x / 23 + 7, z / 23 + 3);
    const arid = sstep(350, 1500, x + (n1 - 0.5) * 500); // rain shadow east of the Mount of Olives ridge
    let west = mix3(C.limestone, C.scrub, sstep(0.45, 0.7, n1 * 0.6 + n2 * 0.5) * (1 - sstep(0.35, 0.8, slope)));
    west = mix3(west, C.terra, sstep(0.62, 0.8, n2) * 0.5 * (1 - sstep(0.2, 0.6, slope)));
    const band = 0.5 + 0.5 * Math.sin(elev / 5.5 + n1 * 5);
    let east = mix3(C.desert, C.desertRock, sstep(0.35, 0.95, slope));
    east = mix3(east, C.strata, band * sstep(0.3, 0.8, slope) * 0.55);
    let col = mix3(west, east, arid);
    col = mix3(col, C.marl, sstep(-120, -220, elev) * (1 - sstep(0.3, 0.8, slope)) * 0.8);
    const oasis = 1 - sstep(260, 520, Math.hypot(x - ariha.x, z - ariha.z) + (n2 - 0.5) * 260);
    col = mix3(col, C.oasis, oasis * 0.85);
    col = mix3(col, C.salt, 1 - sstep(0.3, 4, h - waterY));
    if (h < waterY - 0.2) col = C.seabed;
    const shade = 0.86 + 0.28 * n2;
    const i = (r * texW + c) * 4, l = 1 / Math.hypot(dx, 1, dz);
    albedo[i] = Math.min(255, col[0] * shade); albedo[i + 1] = Math.min(255, col[1] * shade); albedo[i + 2] = Math.min(255, col[2] * shade); albedo[i + 3] = 255;
    normal[i] = (-dx * l * 0.5 + 0.5) * 255; normal[i + 1] = (l * 0.5 + 0.5) * 255; normal[i + 2] = (-dz * l * 0.5 + 0.5) * 255; normal[i + 3] = 255;
  }
  // The road as a dusty line, and a dark dot under every tree: both read well from far away.
  const dab = (x: number, z: number, rad: number, col: number[], k: number) => {
    const c0 = Math.round((x - x0) / ex), r0 = Math.round((z - z0) / ez), n = Math.ceil(rad / ex);
    for (let a = -n; a <= n; a++) for (let b = -n; b <= n; b++) {
      const c = c0 + a, r = r0 + b;
      if (c < 0 || r < 0 || c >= texW || r >= texH) continue;
      const w = k * Math.max(0, 1 - Math.hypot(a * ex, b * ez) / (rad + ex * 0.5)), i = (r * texW + c) * 4;
      for (let j = 0; j < 3; j++) albedo[i + j] += (col[j] - albedo[i + j]) * w;
    }
  };
  if (ex < 10) for (let i = 0; i < world.n; i++) dab(world.px[i], world.pz[i], world.halfWidth + 3, [150, 138, 116], 0.6);
  for (const p of plants) {
    if (p.kind === 'olive') dab(p.x, p.z, 2.6 * p.scale, [72, 84, 52], 0.75);
    else if (p.kind === 'palm') dab(p.x, p.z, 2.4, [60, 86, 44], 0.7);
    else if (p.kind === 'cypress') dab(p.x, p.z, 1.6, [44, 62, 42], 0.8);
  }
  const tex = (data: Uint8Array, srgb: boolean) => {
    const t = new THREE.DataTexture(data, texW, texH, THREE.RGBAFormat);
    t.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
    t.minFilter = THREE.LinearMipmapLinearFilter; t.magFilter = THREE.LinearFilter; t.generateMipmaps = true;
    t.wrapS = t.wrapT = THREE.ClampToEdgeWrapping; t.needsUpdate = true;
    return t;
  };
  return { map: tex(albedo, true), normalMap: tex(normal, false) };
}

function terrainMaterial(tex: { map: THREE.Texture; normalMap: THREE.Texture }, photos: Photos, sizeX: number, sizeZ: number, shade: SunShade) {
  const m = new THREE.MeshStandardMaterial({ map: tex.map, normalMap: tex.normalMap, normalMapType: THREE.ObjectSpaceNormalMap, roughness: 0.94, metalness: 0 });
  m.defines = { RIHLA_DETAIL: '' };
  applySunShade(m, shade, (shader) => {
    const u = shader.uniforms;
    for (const k of ['rock', 'soil', 'mud'] as const) { u[k + 'C'] = { value: photos[k].color }; u[k + 'N'] = { value: photos[k].normal }; u[k + 'Mean'] = { value: photos[k].mean }; }
    u.detailScale = { value: new THREE.Vector2(sizeX / 5, sizeZ / 5) };
    shader.vertexShader = 'varying float vTerrY;\n' + shader.vertexShader.replace('vVisUv = (rihlaWorld.xz', 'vTerrY = rihlaWorld.y;\n       vVisUv = (rihlaWorld.xz');
    shader.fragmentShader = `uniform sampler2D rockC; uniform sampler2D rockN; uniform sampler2D soilC; uniform sampler2D soilN; uniform sampler2D mudC; uniform sampler2D mudN;
      uniform vec3 rockMean; uniform vec3 soilMean; uniform vec3 mudMean; uniform vec2 detailScale; varying float vTerrY; vec2 rihlaDn; float rihlaFade;\n` + shader.fragmentShader
      .replace('#include <map_fragment>', `#include <map_fragment>
        {
          // The baked map gives each place its broad colour. Close to the camera, scanned photographs of
          // rock (steep ground), stony soil (gentler ground) and cracked mud (the valley floor) give the detail.
          #ifdef RIHLA_DETAIL
          vec2 duv = vMapUv * detailScale;
          float steep = smoothstep(0.94, 0.78, texture2D(normalMap, vNormalMapUv).y * 2.0 - 1.0);
          float mud = smoothstep(-100.0, -128.0, vTerrY) * (1.0 - steep);
          vec3 det = mix(mix(texture2D(soilC, duv).rgb / soilMean, texture2D(mudC, duv * 0.7).rgb / mudMean, mud), texture2D(rockC, duv * 0.55).rgb / rockMean, steep);
          vec3 broad = texture2D(soilC, duv * 0.09).rgb / soilMean;
          rihlaDn = mix(mix(texture2D(soilN, duv).xy, texture2D(mudN, duv * 0.7).xy, mud), texture2D(rockN, duv * 0.55).xy, steep) * 2.0 - 1.0;
          rihlaFade = 1.0 - smoothstep(90.0, 700.0, gl_FragCoord.z / gl_FragCoord.w);
          diffuseColor.rgb *= mix(vec3(1.0), det, rihlaFade) * mix(vec3(1.0), broad, 0.55);
          #else
          rihlaDn = vec2(0.0); rihlaFade = 0.0;
          #endif
        }`)
      .replace('#include <normal_fragment_maps>', `
        vec3 rihlaN = texture2D(normalMap, vNormalMapUv).xyz * 2.0 - 1.0;
        rihlaN = normalize(rihlaN + vec3(rihlaDn.x, 0.0, -rihlaDn.y) * 0.9 * rihlaFade);
        normal = normalize(normalMatrix * rihlaN);`);
  });
  return m;
}

export class Terrain {
  readonly group = new THREE.Group();
  /** Multiplier on level-of-detail distances; set by the quality preset. */
  lodScale = 1;
  private readonly near: Heightmap;
  private readonly material: THREE.MeshStandardMaterial;
  private farMaterial!: THREE.MeshStandardMaterial;
  private readonly chunks: { cx: number; cz: number; level: number; mesh: THREE.Mesh | null }[] = [];
  private readonly indexCache = new Map<number, THREE.BufferAttribute>();
  private readonly textures: THREE.Texture[] = [];

  constructor(world: World, plants: Plant[], readonly shade: SunShade, readonly farShade: SunShade, photos: Photos) {
    this.near = world.near;
    const ni = world.near.info, fi = world.far.info, detail = photos;
    const nearTex = bake(world.near, world, ni.cols + 1, ni.rows + 1, plants);
    const farTex = bake(world.far, world, fi.cols * 2 + 1, fi.rows * 2 + 1, []);
    this.textures.push(nearTex.map, nearTex.normalMap, farTex.map, farTex.normalMap);
    this.material = terrainMaterial(nearTex, detail, ni.cols * ni.cell, ni.rows * ni.cell, shade);
    for (let cz = 0; cz < ni.rows / CHUNK; cz++) for (let cx = 0; cx < ni.cols / CHUNK; cx++) this.chunks.push({ cx, cz, level: -1, mesh: null });

    // Far terrain: one mesh. Inside the near rectangle it is sunk so the detailed terrain covers it.
    const fw = fi.cols + 1, fh = fi.rows + 1, pos = new Float32Array(fw * fh * 3), uv = new Float32Array(fw * fh * 2);
    for (let r = 0; r < fh; r++) for (let c = 0; c < fw; c++) {
      const x = fi.x0 + c * fi.cell, z = fi.z0 + r * fi.cell, i = r * fw + c;
      pos[i * 3] = x; pos[i * 3 + 1] = world.far.data[i] - (world.near.contains(x, z, 0.5) ? 26 : 0); pos[i * 3 + 2] = z;
      uv[i * 2] = c / fi.cols; uv[i * 2 + 1] = r / fi.rows;
    }
    const idx = new Uint32Array(fi.cols * fi.rows * 6);
    let k = 0;
    for (let r = 0; r < fi.rows; r++) for (let c = 0; c < fi.cols; c++) {
      const a = r * fw + c, b = a + fw;
      if (world.near.contains(fi.x0 + c * fi.cell, fi.z0 + r * fi.cell, fi.cell * 1.5) && world.near.contains(fi.x0 + (c + 1) * fi.cell, fi.z0 + (r + 1) * fi.cell, fi.cell * 1.5)) continue;
      idx[k++] = a; idx[k++] = b; idx[k++] = a + 1; idx[k++] = b; idx[k++] = b + 1; idx[k++] = a + 1;
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
    g.setIndex(new THREE.BufferAttribute(idx.slice(0, k), 1));
    g.computeBoundingSphere();
    this.farMaterial = terrainMaterial(farTex, detail, fi.cols * fi.cell, fi.rows * fi.cell, farShade);
    const far = new THREE.Mesh(g, this.farMaterial);
    far.receiveShadow = false; far.matrixAutoUpdate = false;
    this.group.add(far);
  }

  /** Photographic detail close to the camera; off on the Low quality level, where it costs too much. */
  setDetail(on: boolean) {
    for (const m of [this.material, this.farMaterial]) {
      const d = (m.defines ??= {});
      if (on === 'RIHLA_DETAIL' in d) continue;
      if (on) d.RIHLA_DETAIL = ''; else delete d.RIHLA_DETAIL;
      m.needsUpdate = true;
    }
  }

  setAnisotropy(a: number) { for (const t of this.textures) { t.anisotropy = a; t.needsUpdate = true; } }

  private indices(n: number): THREE.BufferAttribute {
    let attr = this.indexCache.get(n);
    if (attr) return attr;
    const row = n + 1, out: number[] = [];
    for (let j = 0; j < n; j++) for (let i = 0; i < n; i++) { const a = j * row + i, b = a + row; out.push(a, b, a + 1, b, b + 1, a + 1); }
    // Skirts: a lowered copy of each edge, joined with two-sided quads so cracks between levels never show.
    const base = row * row;
    const edge = (at: (t: number) => number, off: number) => {
      for (let t = 0; t < n; t++) { const a = at(t), b = at(t + 1), c = base + off + t, d = c + 1; out.push(a, b, c, b, d, c, a, c, b, b, c, d); }
    };
    edge((t) => t, 0); edge((t) => n * row + t, row); edge((t) => t * row, row * 2); edge((t) => t * row + n, row * 3);
    attr = new THREE.BufferAttribute(new Uint32Array(out), 1);
    this.indexCache.set(n, attr);
    return attr;
  }

  private build(cx: number, cz: number, level: number): THREE.Mesh {
    const hm = this.near, { x0, z0, cell, cols, rows } = hm.info, stride = STRIDES[level], n = CHUNK / stride, row = n + 1;
    const pos = new Float32Array((row * row + row * 4) * 3), uv = new Float32Array((row * row + row * 4) * 2);
    const put = (i: number, c: number, r: number, drop: number) => {
      pos[i * 3] = x0 + c * cell; pos[i * 3 + 1] = hm.data[r * hm.w + c] - drop; pos[i * 3 + 2] = z0 + r * cell;
      uv[i * 2] = c / cols; uv[i * 2 + 1] = r / rows;
    };
    const c0 = cx * CHUNK, r0 = cz * CHUNK;
    for (let j = 0; j <= n; j++) for (let i = 0; i <= n; i++) put(j * row + i, c0 + i * stride, r0 + j * stride, 0);
    const border = (c: number, r: number) => (c === 0 || r === 0 || c === cols || r === rows ? 32 : 2 + stride * 1.5);
    const base = row * row;
    for (let t = 0; t <= n; t++) {
      const a = c0 + t * stride, b = r0 + t * stride;
      put(base + t, a, r0, border(a, r0)); put(base + row + t, a, r0 + CHUNK, border(a, r0 + CHUNK));
      put(base + row * 2 + t, c0, b, border(c0, b)); put(base + row * 3 + t, c0 + CHUNK, b, border(c0 + CHUNK, b));
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    g.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
    g.setIndex(this.indices(n));
    g.computeBoundingSphere();
    const mesh = new THREE.Mesh(g, this.material);
    mesh.receiveShadow = true; mesh.matrixAutoUpdate = false;
    return mesh;
  }

  /** Choose a level of detail for every chunk from the camera position. Cheap; call a few times a second. */
  update(cam: THREE.Vector3, all = false) {
    const { x0, z0, cell } = this.near.info, size = CHUNK * cell;
    let rebuilt = 0;
    for (const ch of this.chunks) {
      const minX = x0 + ch.cx * size, minZ = z0 + ch.cz * size;
      const dx = Math.max(minX - cam.x, 0, cam.x - minX - size), dz = Math.max(minZ - cam.z, 0, cam.z - minZ - size);
      const d = Math.hypot(dx, dz) / this.lodScale;
      const level = d < 190 ? 0 : d < 520 ? 1 : d < 1150 ? 2 : d < 2300 ? 3 : 4;
      if (level === ch.level) continue;
      if (!all && rebuilt >= 6 && ch.mesh) continue; // spread rebuilding over several updates, except after a jump
      if (ch.mesh) { this.group.remove(ch.mesh); const g = ch.mesh.geometry; g.setIndex(null); g.dispose(); }
      ch.mesh = this.build(ch.cx, ch.cz, level); ch.level = level;
      this.group.add(ch.mesh); rebuilt++;
    }
  }
}
