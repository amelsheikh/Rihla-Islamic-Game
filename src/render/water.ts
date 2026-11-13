// The Dead Sea: pale turquoise in the shallows, deep blue further out, with a white salt rim and slow ripples.
import * as THREE from 'three';
import type { World } from '../world/world';
import { waterNormals } from './textures';

export function buildWater(world: World): { mesh: THREE.Mesh; tick: (t: number) => void } {
  const fi = world.far.info, w = fi.cols + 1, h = fi.rows + 1, depth = new Uint8Array(w * h), waterY = world.data.waterY;
  for (let i = 0; i < w * h; i++) {
    const x = fi.x0 + (i % w) * fi.cell, z = fi.z0 + Math.floor(i / w) * fi.cell;
    depth[i] = Math.min(255, Math.max(0, (waterY - world.terrainY(x, z)) * 28));
  }
  const depthTex = new THREE.DataTexture(depth, w, h, THREE.RedFormat);
  depthTex.minFilter = depthTex.magFilter = THREE.LinearFilter; depthTex.needsUpdate = true;
  const uniforms = { rippleTex: { value: waterNormals() }, depthTex: { value: depthTex }, waterRect: { value: new THREE.Vector4(fi.x0, fi.z0, fi.cols * fi.cell, fi.rows * fi.cell) }, waterTime: { value: 0 } };
  const mat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.08, metalness: 0.05 });
  mat.onBeforeCompile = (shader) => {
    Object.assign(shader.uniforms, uniforms);
    shader.vertexShader = 'varying vec2 vWaterXZ;\n' + shader.vertexShader.replace('#include <project_vertex>', '#include <project_vertex>\n vWaterXZ = (modelMatrix * vec4(transformed, 1.0)).xz;');
    shader.fragmentShader = 'uniform sampler2D rippleTex;\nuniform sampler2D depthTex;\nuniform vec4 waterRect;\nuniform float waterTime;\nvarying vec2 vWaterXZ;\n' + shader.fragmentShader
      .replace('#include <map_fragment>', `
        float wd = texture2D(depthTex, (vWaterXZ - waterRect.xy) / waterRect.zw).r;
        vec3 shallow = vec3(0.30, 0.62, 0.60), deep = vec3(0.035, 0.20, 0.30);
        diffuseColor.rgb = mix(shallow, deep, smoothstep(0.0, 0.75, wd));
        diffuseColor.rgb = mix(vec3(0.86, 0.87, 0.82), diffuseColor.rgb, smoothstep(0.0, 0.06, wd));`)
      .replace('#include <normal_fragment_maps>', `
        vec2 r1 = texture2D(rippleTex, vWaterXZ * 0.035 + vec2(waterTime * 0.010, waterTime * 0.006)).xy * 2.0 - 1.0;
        vec2 r2 = texture2D(rippleTex, vWaterXZ * 0.011 - vec2(waterTime * 0.004, waterTime * 0.007)).xy * 2.0 - 1.0;
        float calm = 1.0 - smoothstep(200.0, 1500.0, gl_FragCoord.z / gl_FragCoord.w);
        normal = normalize((viewMatrix * vec4(normalize(vec3((r1.x + r2.x) * 0.10 * calm, 1.0, (r1.y + r2.y) * 0.10 * calm)), 0.0)).xyz);`);
  };
  const mesh = new THREE.Mesh(new THREE.PlaneGeometry(fi.cols * fi.cell, fi.rows * fi.cell), mat);
  mesh.rotation.x = -Math.PI / 2;
  mesh.position.set(fi.x0 + (fi.cols * fi.cell) / 2, waterY, fi.z0 + (fi.rows * fi.cell) / 2);
  return { mesh, tick: (t) => { uniforms.waterTime.value = t; } };
}
