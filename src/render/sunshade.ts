// Long shadows of hills and valley walls. Real-time shadow maps cannot reach across a 10 km view on a
// modest laptop, so the ground itself is checked against the sun: for every patch of terrain, is there
// higher ground between it and the sun? The answer is kept in a small texture and refreshed, a little
// each frame, as the sun moves. Every material that calls `applySunShade` is darkened by it.
import * as THREE from 'three';
import type { GridInfo } from '../world/heightmap';
import type { World } from '../world/world';

export class SunShade {
  readonly uniforms = {
    visA: { value: null as unknown as THREE.DataTexture }, visB: { value: null as unknown as THREE.DataTexture },
    visMix: { value: 0 }, visRect: { value: new THREE.Vector4() },
    /** Raised at night so that moonlight and the headlamps are not blocked by hill shadows. */
    visFloor: { value: 0 },
  };
  private readonly w: number; private readonly h: number;
  private readonly step: number;
  private readonly back: Uint8Array;
  private readonly baked = new THREE.Vector3(0, -1, 0);
  private readonly target = new THREE.Vector3();
  private row = -1; private blending = false;

  constructor(private readonly world: World, private readonly rect: GridInfo, texelsPerCell: number, private readonly maxDist: number) {
    this.step = rect.cell / texelsPerCell;
    this.w = Math.round(rect.cols * texelsPerCell) + 1; this.h = Math.round(rect.rows * texelsPerCell) + 1;
    this.back = new Uint8Array(this.w * this.h);
    const make = () => {
      const t = new THREE.DataTexture(new Uint8Array(this.w * this.h).fill(255), this.w, this.h, THREE.RedFormat);
      t.minFilter = t.magFilter = THREE.LinearFilter; t.needsUpdate = true;
      return t;
    };
    this.uniforms.visA.value = make(); this.uniforms.visB.value = make();
    this.uniforms.visRect.value.set(rect.x0, rect.z0, rect.cols * rect.cell, rect.rows * rect.cell);
  }

  private bakeRow(r: number, dir: THREE.Vector3) {
    const { x0, z0 } = this.rect, world = this.world, hl = Math.hypot(dir.x, dir.z) || 1e-6;
    const dx = dir.x / hl, dz = dir.z / hl, tan = dir.y / hl, z = z0 + r * this.step, out = this.back, o = r * this.w;
    if (dir.y <= 0.004) { out.fill(0, o, o + this.w); return; }
    const top = this.rect.yMax + 5;
    for (let c = 0; c < this.w; c++) {
      const x = x0 + c * this.step, y0 = world.terrainY(x, z) + 0.6;
      let vis = 1;
      for (let t = 5; t < this.maxDist; t *= 1.28) {
        const ray = y0 + t * tan;
        if (ray > top) break;
        const clear = (ray - world.terrainY(x + dx * t, z + dz * t)) / (t * 0.03) + 0.5;
        if (clear < vis) { vis = clear; if (vis <= 0) { vis = 0; break; } }
      }
      out[o + c] = vis * 255;
    }
  }

  /** Bake everything now (used once at load). */
  bakeNow(dir: THREE.Vector3) {
    for (let r = 0; r < this.h; r++) this.bakeRow(r, dir);
    for (const k of ['visA', 'visB'] as const) { (this.uniforms[k].value.image.data as Uint8Array).set(this.back); this.uniforms[k].value.needsUpdate = true; }
    this.baked.copy(dir); this.row = -1; this.blending = false; this.uniforms.visMix.value = 0;
  }

  /** Spend up to `budgetMs` refreshing the shadows if the light has moved. */
  update(dt: number, dir: THREE.Vector3, budgetMs: number) {
    const u = this.uniforms;
    if (this.blending) {
      u.visMix.value = Math.min(1, u.visMix.value + dt / 1.2);
      if (u.visMix.value >= 1) {
        const a = u.visA.value; u.visA.value = u.visB.value; u.visB.value = a; u.visMix.value = 0; this.blending = false;
      }
      return;
    }
    if (this.row < 0) {
      if (this.baked.angleTo(dir) < 0.02) return;
      this.target.copy(dir); this.row = 0;
    }
    const end = performance.now() + budgetMs;
    while (this.row < this.h && performance.now() < end) this.bakeRow(this.row++, this.target);
    if (this.row >= this.h) {
      (u.visB.value.image.data as Uint8Array).set(this.back); u.visB.value.needsUpdate = true;
      this.baked.copy(this.target); this.row = -1; this.blending = true;
    }
  }
}

/** Make a material obey the hill shadows. `ownUv` is for the terrain, whose texture already spans the grid. */
export function applySunShade(material: THREE.Material, shade: SunShade, extra?: (shader: THREE.WebGLProgramParametersWithUniforms) => void) {
  material.onBeforeCompile = (shader) => {
    Object.assign(shader.uniforms, shade.uniforms);
    shader.vertexShader = 'uniform vec4 visRect;\nvarying vec2 vVisUv;\n' + shader.vertexShader.replace(
      '#include <project_vertex>',
      `#include <project_vertex>
       vec4 rihlaWorld = vec4(transformed, 1.0);
       #ifdef USE_INSTANCING
         rihlaWorld = instanceMatrix * rihlaWorld;
       #endif
       rihlaWorld = modelMatrix * rihlaWorld;
       vVisUv = (rihlaWorld.xz - visRect.xy) / visRect.zw;`,
    );
    shader.fragmentShader = 'uniform sampler2D visA;\nuniform sampler2D visB;\nuniform float visMix;\nuniform float visFloor;\nvarying vec2 vVisUv;\n' + shader.fragmentShader.replace(
      '#include <aomap_fragment>',
      `float rihlaVis = max(visFloor, mix(texture2D(visA, vVisUv).r, texture2D(visB, vVisUv).r, visMix));
       reflectedLight.directDiffuse *= rihlaVis; reflectedLight.directSpecular *= rihlaVis;
       #include <aomap_fragment>`,
    );
    extra?.(shader);
  };
  // Materials sharing a program must share these edits.
  material.customProgramCacheKey = () => 'sunshade' + (extra ? extra.toString().length : 0) + shade.uniforms.visRect.value.z;
}
