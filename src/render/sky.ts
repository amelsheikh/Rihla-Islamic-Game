// Sky, sun, moon, stars, fog and the light they cast, driven by the time of day at the latitude of Al-Quds.
import * as THREE from 'three';
import { Sky } from 'three/addons/objects/Sky.js';
import { ORIGIN } from '../world/warp';

const DEG = Math.PI / 180;
const sstep = (a: number, b: number, x: number) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

export class SkySystem {
  /** Hours, 0..24, local solar time. */
  time = 8.5;
  /** Game seconds per real second. */
  timeScale = 60;
  dayOfYear = 95; // early April: spring in the hills
  readonly sunDir = new THREE.Vector3();
  /** Direction to whichever of the sun and moon is lighting the land. */
  readonly lightDir = new THREE.Vector3(0, 1, 0);
  readonly light = new THREE.DirectionalLight(0xffffff, 3);
  readonly hemi = new THREE.HemisphereLight(0xbfd4ff, 0x8a7a5c, 0.5);
  /** 0 by day, 1 in full night. */
  night = 0;
  shadowRange = 85;
  private readonly sky = new Sky();
  private readonly stars: THREE.Points;
  private readonly moon: THREE.Mesh;
  private readonly moonDir = new THREE.Vector3();
  private readonly envScene = new THREE.Scene();
  private readonly envSky = new Sky();
  private readonly pmrem: THREE.PMREMGenerator;
  private envTarget: THREE.WebGLRenderTarget | null = null;
  private envSun = new THREE.Vector3(9, 9, 9);
  useEnvironment = true;

  constructor(private readonly scene: THREE.Scene, private readonly renderer: THREE.WebGLRenderer) {
    this.sky.scale.setScalar(14000);
    this.sky.material.uniforms.cloudCoverage.value = 0.32;
    this.envSky.material.uniforms.cloudCoverage.value = 0.32;
    this.sky.frustumCulled = false;
    scene.add(this.sky);
    this.envSky.scale.setScalar(1000);
    this.envScene.add(this.envSky);
    this.pmrem = new THREE.PMREMGenerator(renderer);

    const N = 1600, pos = new Float32Array(N * 3), col = new Float32Array(N * 3);
    let seed = 7;
    const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);
    for (let i = 0; i < N; i++) {
      const u = rnd() * 2 - 1, a = rnd() * Math.PI * 2, s = Math.sqrt(1 - u * u), b = 0.35 + rnd() ** 3 * 0.65;
      pos.set([Math.cos(a) * s * 11000, Math.abs(u) * 11000 + 100, Math.sin(a) * s * 11000], i * 3);
      col.set([b, b, b * (0.85 + rnd() * 0.3)], i * 3);
    }
    const sg = new THREE.BufferGeometry();
    sg.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    sg.setAttribute('color', new THREE.BufferAttribute(col, 3));
    this.stars = new THREE.Points(sg, new THREE.PointsMaterial({ size: 2.2, sizeAttenuation: false, vertexColors: true, transparent: true, depthWrite: false, fog: false }));
    this.stars.frustumCulled = false;
    scene.add(this.stars);

    // A crescent moon, drawn once on a canvas.
    const cv = document.createElement('canvas'); cv.width = cv.height = 128;
    const g = cv.getContext('2d')!;
    g.fillStyle = '#fff8e0'; g.beginPath(); g.arc(64, 64, 50, 0, 7); g.fill();
    g.globalCompositeOperation = 'destination-out'; g.beginPath(); g.arc(84, 54, 46, 0, 7); g.fill();
    this.moon = new THREE.Mesh(new THREE.PlaneGeometry(520, 520), new THREE.MeshBasicMaterial({ map: new THREE.CanvasTexture(cv), transparent: true, depthWrite: false, fog: false }));
    scene.add(this.moon);

    this.light.shadow.bias = -0.0004; this.light.shadow.normalBias = 0.06;
    scene.add(this.light, this.light.target, this.hemi);
    scene.fog = new THREE.FogExp2(0xc9d8e6, 0.0001);
  }

  setShadows(size: number) {
    this.light.castShadow = size > 0;
    if (size > 0 && this.light.shadow.mapSize.x !== size) {
      this.light.shadow.mapSize.set(size, size);
      this.light.shadow.map?.dispose(); this.light.shadow.map = null as any;
    }
  }

  update(dt: number, focus: THREE.Vector3, camera: THREE.Camera) {
    this.time = (this.time + (dt * this.timeScale) / 3600 + 24) % 24;
    const lat = ORIGIN.lat * DEG, decl = -23.44 * DEG * Math.cos((2 * Math.PI * (this.dayOfYear + 10)) / 365), H = (this.time - 12) * 15 * DEG;
    const up = Math.sin(lat) * Math.sin(decl) + Math.cos(lat) * Math.cos(decl) * Math.cos(H);
    const east = -Math.cos(decl) * Math.sin(H), north = Math.cos(lat) * Math.sin(decl) - Math.sin(lat) * Math.cos(decl) * Math.cos(H);
    this.sunDir.set(east, up, -north).normalize();
    const day = sstep(-0.1, 0.12, up), high = sstep(0, 0.45, up);
    this.night = 1 - sstep(-0.22, -0.02, up);
    this.moonDir.set(-east * 0.8 + 0.2, Math.max(0.3, 0.55 - up * 0.6), north * 0.8).normalize();

    const u = this.sky.material.uniforms;
    u.sunPosition.value.copy(this.sunDir);
    u.turbidity.value = 3.2; u.rayleigh.value = 1.2 + (1 - high) * 1.6; u.mieCoefficient.value = 0.004; u.mieDirectionalG.value = 0.82;

    // One shadow-casting light: the sun by day, the moon at night, fading through zero at the changeover.
    const sunUp = up > -0.06, dir = sunUp ? this.sunDir : this.moonDir;
    this.lightDir.copy(dir);
    const warm = new THREE.Color(1.0, 0.56, 0.3).lerp(new THREE.Color(1.0, 0.96, 0.9), high);
    if (sunUp) { this.light.color.copy(warm); this.light.intensity = 3.0 * sstep(-0.06, 0.08, up); }
    else { this.light.color.setRGB(0.62, 0.72, 1.0); this.light.intensity = 0.9 * sstep(-0.06, -0.2, up); }
    const r = this.shadowRange, texel = (2 * r) / this.light.shadow.mapSize.x;
    this.light.target.position.set(Math.round(focus.x / texel) * texel, focus.y, Math.round(focus.z / texel) * texel);
    this.light.position.copy(this.light.target.position).addScaledVector(dir, 400);
    const sc = this.light.shadow.camera;
    if (sc.right !== r) { sc.left = -r; sc.right = r; sc.top = r; sc.bottom = -r; sc.near = 50; sc.far = 800; sc.updateProjectionMatrix(); }

    this.hemi.color.setRGB(0.55, 0.68, 1.0).lerp(new THREE.Color(0.2, 0.26, 0.5), this.night);
    this.hemi.groundColor.setRGB(0.55, 0.47, 0.36).lerp(new THREE.Color(0.07, 0.08, 0.12), this.night);
    this.hemi.intensity = (this.useEnvironment ? 0.3 : 0.9) * (0.5 + 0.5 * day);

    const fog = this.scene.fog as THREE.FogExp2;
    const dusk = new THREE.Color(0.8, 0.52, 0.36), noon = new THREE.Color(0.56, 0.68, 0.84), dark = new THREE.Color(0.012, 0.018, 0.04);
    fog.density = 0.00009 * (1 + 1.8 * sstep(60, -150, camera.position.y));
    fog.color.copy(dusk).lerp(noon, high).multiplyScalar(0.25 + 0.75 * day).lerp(dark, this.night);

    (this.stars.material as THREE.PointsMaterial).opacity = this.night;
    this.stars.visible = this.night > 0.01;
    this.stars.position.set(camera.position.x, 0, camera.position.z);
    this.sky.position.copy(camera.position);
    this.moon.visible = this.night > 0.01;
    (this.moon.material as THREE.MeshBasicMaterial).opacity = this.night;
    this.moon.position.copy(camera.position).addScaledVector(this.moonDir, 10000);
    this.moon.lookAt(camera.position);
    this.renderer.toneMappingExposure = 0.4 + 0.74 * (1 - sstep(-0.02, 0.3, up));
    u.time.value += dt * 40;

    // Re-capture the sky as ambient light and reflections when the sun has moved enough.
    if (this.useEnvironment && this.envSun.angleTo(this.sunDir) > 1.5 * DEG) {
      this.envSun.copy(this.sunDir);
      const eu = this.envSky.material.uniforms;
      for (const k of ['turbidity', 'rayleigh', 'mieCoefficient', 'mieDirectionalG']) eu[k].value = u[k].value;
      eu.sunPosition.value.copy(this.sunDir);
      const old = this.envTarget;
      this.envTarget = this.pmrem.fromScene(this.envScene, 0.04, 1, 2000);
      this.scene.environment = this.envTarget.texture;
      this.scene.environmentIntensity = 0.42;
      old?.dispose();
    } else if (!this.useEnvironment && this.scene.environment) this.scene.environment = null;
  }
}
