// Rihla: Roads Without Borders. Phase 1a grey-box build.
import '@fontsource/amiri/arabic-400.css';
import '@fontsource/amiri/latin-400.css';
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import { Sound } from './audio/sound';
import { Input } from './core/input';
import { guessQuality, lang, loadProgress, loadSettings, num, PRESETS, QUALITIES, saveProgress, saveSettings, setLang, t, type Quality } from './core/settings';
import { pageOpen, PAGES, showIntro, showPage } from './game/journal';
import farUrl from './generated/far.rhm?url';
import nearUrl from './generated/near.rhm?url';
import worldJson from './generated/world.json';
import { Autopilot } from './physics/autopilot';
import { Vehicle, type Controls } from './physics/vehicle';
import { buildPlaces } from './render/buildings';
import { PostFx } from './render/postfx';
import { buildRoad, CarModel, Labels } from './render/scenery';
import { SunShade } from './render/sunshade';
import { placePlants, Vegetation } from './render/vegetation';
import { buildWater } from './render/water';
import { SkySystem } from './render/sky';
import { Terrain } from './render/terrain';
import './ui/style.css';
import { Heightmap } from './world/heightmap';
import { toRealElevation } from './world/warp';
import { World, type WorldData } from './world/world';

const $ = <T extends HTMLElement = HTMLElement>(id: string) => document.getElementById(id) as T;
const params = new URLSearchParams(location.search);
const settings = loadSettings();
if (params.get('lang') === 'ar' || params.get('lang') === 'en') settings.lang = params.get('lang') as 'ar' | 'en';

function applyText() {
  setLang(settings.lang);
  for (const el of document.querySelectorAll<HTMLElement>('[data-t]')) {
    if (el.id === 'start') continue;
    el.textContent = t(el.dataset.t!);
  }
  document.title = t('title');
}
applyText();
$('start').textContent = t('loading');

async function boot() {
  const data = worldJson as unknown as WorldData;
  const [, nearBuf, farBuf] = await Promise.all([
    RAPIER.init(),
    fetch(nearUrl).then((r) => r.arrayBuffer()),
    fetch(farUrl).then((r) => r.arrayBuffer()),
    document.fonts.load('40px Amiri', 'القدس').catch(() => null),
  ]);
  const world = new World(data, await Heightmap.decode(data.near, nearBuf), await Heightmap.decode(data.far, farBuf));

  // ---------------------------------------------------------------- renderer and scene
  const canvas = $<HTMLCanvasElement>('view');
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: !matchMedia('(pointer: coarse)').matches, powerPreference: 'high-performance' });
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFShadowMap;
  renderer.info.autoReset = false; // counted per frame, across all passes
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(58, 1, 0.4, 16000);
  const sky = new SkySystem(scene, renderer);
  const post = new PostFx(renderer, scene, camera);
  // Hill shadows: a fine grid for the land around the road, a coarse one for the far landscape.
  const shade = new SunShade(world, world.near.info, 0.5, 2600), farShade = new SunShade(world, world.far.info, 1, 7000);
  const water = buildWater(world);
  scene.add(water.mesh);

  // ---------------------------------------------------------------- physics
  const DT = 1 / 120;
  const rapier = new RAPIER.World({ x: 0, y: -9.81, z: 0 });
  rapier.timestep = DT;
  const car = new Vehicle(rapier, world);
  const START = 6;
  car.mode = settings.mode;
  car.placeOnRoad(START);
  const pilot = new Autopilot(world, car);
  const plants = placePlants(world);
  const terrain = new Terrain(world, plants, shade, farShade);
  const vegetation = new Vegetation(world, plants, shade, rapier);
  const places = buildPlaces(world, rapier, shade);
  const labels = new Labels(places.named);
  scene.add(terrain.group, buildRoad(world, shade), vegetation.group, places.group, labels.group);
  sky.update(0, new THREE.Vector3(), camera);
  shade.bakeNow(sky.lightDir); farShade.bakeNow(sky.lightDir);

  // The first page of the Rihla waits beside the road at Khan al-Hatruri, as a golden scroll on a stone.
  const progress = loadProgress(), page = PAGES[0], sound = new Sound();
  sound.volume = settings.volume;
  const pageMarker = new THREE.Group();
  {
    const khan = world.data.places.find((p) => p.id === page.place)!, r = world.roadAt(khan.roadDist);
    const side = Math.sign((khan.x - r.x) * -r.dz + (khan.z - r.z) * r.dx) || 1, off = world.halfWidth + 3.2;
    const x = r.x - r.dz * off * side, z = r.z + r.dx * off * side, y = world.groundY(x, z);
    const plinth = new THREE.Mesh(new THREE.CylinderGeometry(0.7, 0.85, 1, 8), new THREE.MeshStandardMaterial({ color: 0xcfc3a4, roughness: 0.9 }));
    plinth.position.y = 0.5; plinth.castShadow = true;
    const scroll = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.16, 0.95, 12), new THREE.MeshStandardMaterial({ color: 0xf2dfa0, emissive: 0xd9a93a, emissiveIntensity: 1.6, roughness: 0.5 }));
    scroll.rotation.z = Math.PI / 2; scroll.position.y = 1.75; scroll.name = 'scroll';
    const beam = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 0.9, 26, 12, 1, true), new THREE.MeshBasicMaterial({ color: 0xffd98a, transparent: true, opacity: 0.16, depthWrite: false, side: THREE.DoubleSide, blending: THREE.AdditiveBlending, fog: false }));
    beam.position.y = 14; beam.name = 'beam';
    pageMarker.add(plinth, scroll, beam); pageMarker.position.set(x, y, z);
    scene.add(pageMarker);
    rapier.createCollider(RAPIER.ColliderDesc.cylinder(0.5, 0.8).setTranslation(x, y + 0.5, z));
  }
  const pageFound = () => progress.pages.includes(page.id);
  const carModel = new CarModel();
  scene.add(carModel.group);

  // ---------------------------------------------------------------- quality
  let quality: Quality = settings.quality === 'auto' ? guessQuality() : settings.quality, shadeBudget = 1.5;
  function applyQuality(q: Quality) {
    quality = q;
    const p = PRESETS[q];
    renderer.setPixelRatio(Math.min(devicePixelRatio || 1, p.pixelRatio));
    renderer.setSize(innerWidth, innerHeight, false);
    sky.setShadows(p.shadows); sky.useEnvironment = p.environment;
    terrain.lodScale = vegetation.lodScale = p.lod;
    shadeBudget = p.shadeBudget;
    post.configure(p.postfx, innerWidth, innerHeight, renderer.getPixelRatio());
    terrain.setAnisotropy(Math.min(p.anisotropy, renderer.capabilities.getMaxAnisotropy()));
  }
  applyQuality(quality);
  addEventListener('resize', () => { renderer.setSize(innerWidth, innerHeight, false); post.configure(PRESETS[quality].postfx, innerWidth, innerHeight, renderer.getPixelRatio()); camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); });
  camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix();

  // ---------------------------------------------------------------- state
  const input = new Input($('touch'));
  const ctl: Controls = { throttle: 0, brake: 0, steer: 0, handbrake: false };
  let paused = true, autopilot = false, camMode: 'chase' | 'bonnet' | 'free' = 'chase', bench: Bench | null = null;
  const prevPos = new THREE.Vector3(), currPos = new THREE.Vector3(), pos = new THREE.Vector3();
  const prevQ = new THREE.Quaternion(), currQ = new THREE.Quaternion(), quat = new THREE.Quaternion();
  const readPose = () => { const p = car.body.translation(), r = car.body.rotation(); currPos.set(p.x, p.y, p.z); currQ.set(r.x, r.y, r.z, r.w); };
  const snapPose = () => { readPose(); prevPos.copy(currPos); prevQ.copy(currQ); camSnap = true; };
  let camSnap = true;
  snapPose();
  const camPos = new THREE.Vector3(), camLook = new THREE.Vector3(), fwd = new THREE.Vector3(), v3 = new THREE.Vector3();
  let freeYaw = 0, freePitch = -0.2;

  // ---------------------------------------------------------------- HUD
  $('journal-btn').hidden = !pageFound();
  const hud = { speed: $('speed'), alt: $('alt'), trip: $('trip'), placeAr: $('place-ar'), placeEn: $('place-en'), clock: $('clock-time'), compass: $('compass'), hint: $('hint'), banner: $('banner'), debug: $('debug') };
  let bannerTimer = 0, lastPlace = '', lastElev = 1e9;
  function banner(title: string, sub: string, seconds = 5) {
    $('banner-title').textContent = title; $('banner-sub').textContent = sub;
    hud.banner.classList.add('show'); bannerTimer = seconds;
  }
  const ariha = world.data.places.find((p) => p.id === 'ariha')!;
  function updateHud() {
    hud.speed.textContent = num(Math.abs(car.speed) * 3.6);
    const elev = toRealElevation(currPos.y - 1.05);
    hud.alt.textContent = `${num(Math.abs(elev))} ${t(elev >= 0 ? 'above' : 'below')}`;
    const hit = world.nearestRoad(currPos.x, currPos.z);
    const left = hit ? ariha.roadDist - hit.along : -1;
    const toPage = Math.hypot(pageMarker.position.x - currPos.x, pageMarker.position.z - currPos.z);
    if (!pageFound()) hud.trip.textContent = toPage < 14 ? t('pageNear') : `${t('objective')} · ${num(Math.abs(world.data.places.find((p) => p.id === page.place)!.roadDist - (hit?.along ?? 0)) / 1000, 1)} km`;
    else hud.trip.textContent = left > 30 ? `${num(left / 1000, 1)} km ${t('toAriha')}` : '';
    // Pick up the page by stopping beside it.
    if (!pageFound() && toPage < 11 && Math.abs(car.speed) < 2.5 && !autopilot) {
      progress.pages.push(page.id); saveProgress(progress);
      $('journal-btn').hidden = false; paused = true;
      showPage(page, () => { paused = false; banner(t('pageFound'), page.title[lang()]); });
    }
    const near = world.nearestPlace(currPos.x, currPos.z);
    hud.placeAr.textContent = near.place.name.ar; hud.placeEn.textContent = near.place.name.en;
    const h = Math.floor(sky.time), m = Math.floor((sky.time - h) * 60);
    hud.clock.textContent = `${num(h).padStart(2, num(0))}:${num(m).padStart(2, num(0))}`;
    fwd.set(0, 0, -1).applyQuaternion(currQ);
    const bearing = (Math.atan2(fwd.x, -fwd.z) * 180) / Math.PI;
    hud.compass.textContent = (lang() === 'ar' ? ['ش', 'ش ق', 'ق', 'ج ق', 'ج', 'ج غ', 'غ', 'ش غ'] : ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'])[Math.round(((bearing + 360) % 360) / 45) % 8];
    hud.hint.textContent = car.needsRescue ? t('rescue') : '';
    // In Family mode the car is put back by itself after a few seconds.
    if (car.mode === 'family' && car.tippedFor > 4) resetCar();
    if (near.dist < 90 && near.place.id !== lastPlace && near.place.kind !== 'nature') { lastPlace = near.place.id; banner(lang() === 'ar' ? near.place.name.ar : near.place.name.en, lang() === 'ar' ? near.place.name.en : near.place.name.ar); }
    // Crossing sea level on the way down (not after a jump to another place).
    if (lastElev >= 0 && lastElev < 2 && elev < 0 && elev > -2 && hit) banner(`${t('seaLevel')} · ${num(0)}`, t('seaLevelSub'), 8);
    lastElev = elev;
  }
  function updateDebug(fps: number, ms: number) {
    const i = renderer.info, mem = (performance as any).memory;
    hud.debug.textContent = `${fps.toFixed(0)} fps  ${ms.toFixed(1)} ms  quality ${quality}\n` +
      `draw calls ${i.render.calls}  triangles ${(i.render.triangles / 1000).toFixed(0)}k\n` +
      `geometries ${i.memory.geometries}  textures ${i.memory.textures}` + (mem ? `  heap ${(mem.usedJSHeapSize / 1048576).toFixed(0)} MB` : '') + '\n' +
      `x ${currPos.x.toFixed(0)}  y ${currPos.y.toFixed(1)}  z ${currPos.z.toFixed(0)}  road ${pilot.progress.toFixed(0)} m\n` +
      `gear ${car.gear}  rpm ${car.rpm.toFixed(0)}  ${car.wheels.map((w) => (w.grounded ? w.surface[0] : '-')).join('')}`;
  }

  // ---------------------------------------------------------------- menu
  const resetCar = () => { car.rescue(); pilot.sync(); snapPose(); banner(t('rescued'), '', 2.5); };
  $('reset-btn').onclick = () => { if (!paused) resetCar(); ($('reset-btn') as HTMLButtonElement).blur(); };
  $('journal-btn').onclick = () => { if (pageFound() && !paused) { paused = true; showPage(page, () => { paused = false; }); } };
  const overlay = $('overlay'), startBtn = $<HTMLButtonElement>('start');
  const sel = { lang: $<HTMLSelectElement>('set-lang'), mode: $<HTMLSelectElement>('set-mode'), quality: $<HTMLSelectElement>('set-quality'), time: $<HTMLInputElement>('set-time'), flow: $<HTMLSelectElement>('set-flow'), labels: $<HTMLInputElement>('set-labels'), volume: $<HTMLInputElement>('set-volume') };
  let started = false;
  function openMenu(open: boolean) {
    paused = open; overlay.hidden = !open; $('hud').hidden = open;
    if (open) { sel.time.value = String(sky.time); startBtn.textContent = t(started ? 'resume' : 'start'); }
    else started = true;
  }
  sel.lang.value = settings.lang; sel.mode.value = settings.mode; sel.quality.value = settings.quality; sel.labels.checked = settings.labels; sel.flow.value = String(sky.timeScale);
  labels.group.visible = settings.labels;
  sel.lang.onchange = () => { settings.lang = sel.lang.value as 'en' | 'ar'; saveSettings(settings); applyText(); startBtn.textContent = t(started ? 'resume' : 'start'); lastPlace = ''; };
  sel.mode.onchange = () => { settings.mode = car.mode = sel.mode.value as 'family' | 'simulation'; saveSettings(settings); };
  sel.quality.onchange = () => { settings.quality = sel.quality.value as Quality | 'auto'; saveSettings(settings); applyQuality(settings.quality === 'auto' ? guessQuality() : settings.quality); };
  sel.time.oninput = () => { sky.time = +sel.time.value % 24; };
  sel.flow.onchange = () => { sky.timeScale = +sel.flow.value; };
  sel.labels.onchange = () => { settings.labels = labels.group.visible = sel.labels.checked; saveSettings(settings); };
  sel.volume.value = String(settings.volume);
  sel.volume.oninput = () => { settings.volume = sound.volume = +sel.volume.value; saveSettings(settings); };
  startBtn.onclick = () => {
    sound.start(); openMenu(false);
    if (!progress.introSeen && !bench) { paused = true; showIntro(() => { progress.introSeen = true; saveProgress(progress); paused = false; }); }
  };
  $('menu-btn').onclick = () => openMenu(true);
  $('restart').onclick = () => { car.placeOnRoad(START); pilot.sync(); snapPose(); lastPlace = ''; openMenu(false); };
  $('bench').onclick = () => startBench();
  startBtn.disabled = false; startBtn.textContent = t('start');

  // ---------------------------------------------------------------- performance test
  interface Bench { index: number; elapsed: number; frames: number[]; calls: number; tris: number; samples: number; rows: string[]; saved: { quality: Quality; time: number; scale: number } }
  function gpuName() {
    const gl = renderer.getContext(), ext = gl.getExtension('WEBGL_debug_renderer_info');
    return ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER);
  }
  function startBench() {
    overlay.hidden = true; $('hud').hidden = false; $('bench-panel').hidden = false;
    $('bench-result').hidden = $('bench-copy').hidden = $('bench-close').hidden = true;
    bench = { index: 0, elapsed: 0, frames: [], calls: 0, tris: 0, samples: 0, rows: [], saved: { quality, time: sky.time, scale: sky.timeScale } };
    benchStage();
  }
  function benchStage() {
    const b = bench!;
    applyQuality(QUALITIES[b.index]);
    sky.time = 16.5; sky.timeScale = 0;
    car.placeOnRoad(120); pilot.sync(); snapPose();
    autopilot = true; paused = false; camMode = 'chase';
    b.elapsed = 0; b.frames = []; b.calls = b.tris = b.samples = 0;
    $('bench-status').textContent = `${t('bench.running')} (${b.index + 1}/${QUALITIES.length})`;
  }
  function benchFrame(dt: number) {
    const b = bench!;
    b.elapsed += dt;
    if (b.elapsed < 3) return; // let the scene settle
    b.frames.push(dt); b.calls += renderer.info.render.calls; b.tris += renderer.info.render.triangles; b.samples++;
    if (b.elapsed < 18) return;
    const sorted = [...b.frames].sort((x, y) => y - x), avg = b.frames.reduce((s, x) => s + x, 0) / b.frames.length;
    const low = sorted.slice(0, Math.max(1, Math.floor(sorted.length / 100))).reduce((s, x) => s + x, 0) / Math.max(1, Math.floor(sorted.length / 100));
    b.rows.push(`${QUALITIES[b.index].padEnd(7)} avg ${(1 / avg).toFixed(0).padStart(3)} fps | 1% low ${(1 / low).toFixed(0).padStart(3)} fps | calls ${(b.calls / b.samples).toFixed(0)} | tris ${(b.tris / b.samples / 1000).toFixed(0)}k`);
    if (++b.index < QUALITIES.length) return benchStage();
    const text = [`RIHLA-BENCH 1b`, `${innerWidth}x${innerHeight} dpr ${devicePixelRatio}`, String(gpuName()), navigator.userAgent, ...b.rows].join('\n');
    applyQuality(b.saved.quality); sky.time = b.saved.time; sky.timeScale = b.saved.scale;
    autopilot = false; paused = true; bench = null;
    $('bench-status').textContent = t('bench.done');
    const ta = $<HTMLTextAreaElement>('bench-result'); ta.value = text; ta.hidden = false;
    $('bench-copy').hidden = $('bench-close').hidden = false;
    (window as any).__rihla.benchResult = text;
  }
  $('bench-copy').onclick = () => { const ta = $<HTMLTextAreaElement>('bench-result'); ta.select(); navigator.clipboard?.writeText(ta.value).catch(() => document.execCommand('copy')); };
  $('bench-close').onclick = () => { $('bench-panel').hidden = true; openMenu(true); };

  // ---------------------------------------------------------------- free camera look
  let dragging = false;
  canvas.addEventListener('pointerdown', () => (dragging = true));
  addEventListener('pointerup', () => (dragging = false));
  addEventListener('pointermove', (e) => { if (dragging && camMode === 'free') { freeYaw -= e.movementX * 0.004; freePitch = Math.max(-1.5, Math.min(1.5, freePitch - e.movementY * 0.004)); } });

  function updateCamera(dt: number) {
    fwd.set(0, 0, -1).applyQuaternion(quat);
    if (camMode === 'free') {
      const k = input.keys, sp = (k.has('ShiftLeft') ? 400 : 60) * dt;
      v3.set(-Math.sin(freeYaw) * Math.cos(freePitch), Math.sin(freePitch), -Math.cos(freeYaw) * Math.cos(freePitch));
      if (k.has('KeyW')) camera.position.addScaledVector(v3, sp);
      if (k.has('KeyS')) camera.position.addScaledVector(v3, -sp);
      const right = new THREE.Vector3(Math.cos(freeYaw), 0, -Math.sin(freeYaw));
      if (k.has('KeyD')) camera.position.addScaledVector(right, sp);
      if (k.has('KeyA')) camera.position.addScaledVector(right, -sp);
      if (k.has('KeyE')) camera.position.y += sp;
      if (k.has('KeyQ')) camera.position.y -= sp;
      camera.position.y = Math.max(camera.position.y, world.groundY(camera.position.x, camera.position.z) + 1);
      camera.lookAt(v3.add(camera.position));
      return;
    }
    if (camMode === 'bonnet') {
      camera.position.set(0, 1.3, -0.35).applyQuaternion(quat).add(pos);
      camera.quaternion.copy(quat);
      camera.fov = 68; camera.updateProjectionMatrix();
      return;
    }
    const flat = Math.hypot(fwd.x, fwd.z) || 1;
    v3.set(pos.x - (fwd.x / flat) * 8.2, pos.y + 3.1, pos.z - (fwd.z / flat) * 8.2);
    const k = camSnap ? 1 : 1 - Math.exp(-dt * 4.5);
    camPos.lerp(v3, k);
    camPos.y = Math.max(camPos.y, world.groundY(camPos.x, camPos.z) + 1.2);
    v3.copy(pos).addScaledVector(fwd, 5); v3.y += 1.3;
    camLook.lerp(v3, camSnap ? 1 : 1 - Math.exp(-dt * 9));
    camSnap = false;
    camera.position.copy(camPos); camera.lookAt(camLook);
    const fov = 56 + Math.min(14, Math.abs(car.speed) * 0.55);
    if (Math.abs(fov - camera.fov) > 0.05) { camera.fov += (fov - camera.fov) * Math.min(1, dt * 3); camera.updateProjectionMatrix(); }
  }

  // ---------------------------------------------------------------- main loop
  let last = performance.now(), acc = 0, frame = 0, fpsTime = 0, fpsFrames = 0, fps = 60, slowFor = 0;
  function tick(now: number) {
    requestAnimationFrame(tick);
    const rawDt = (now - last) / 1000, dt = Math.min(0.1, rawDt); last = now; frame++;

    if (input.hit('Escape') && started && !bench && !pageOpen()) openMenu(!paused);
    if (input.hit('F3') || input.hit('Backquote')) hud.debug.hidden = !hud.debug.hidden;
    if (!paused && !bench) {
      if (input.hit('KeyC')) { camMode = camMode === 'chase' ? 'bonnet' : 'chase'; camSnap = true; }
      if (input.hit('KeyF')) { camMode = camMode === 'free' ? 'chase' : 'free'; camSnap = true; freeYaw = Math.atan2(-fwd.x, -fwd.z); }
      if (input.hit('KeyR')) resetCar();
      if (input.hit('KeyH')) sound.horn();
      if (input.hit('KeyJ')) $('journal-btn').click();
      if (input.hit('KeyP')) { autopilot = !autopilot; pilot.sync(); }
      if (input.hit('BracketRight')) sky.time = (sky.time + 1) % 24;
      if (input.hit('BracketLeft')) sky.time = (sky.time + 23) % 24;
      if (input.hit('KeyN')) labels.group.visible = !labels.group.visible;
    }
    input.endFrame();

    if (!paused) {
      acc += dt;
      let steps = 0;
      while (acc >= DT && steps < 10) {
        if (autopilot) pilot.update(ctl);
        else if (camMode === 'free') { ctl.throttle = ctl.steer = 0; ctl.brake = 1; ctl.handbrake = true; }
        else input.read(ctl);
        prevPos.copy(currPos); prevQ.copy(currQ);
        car.step(DT, ctl); rapier.step(); readPose();
        acc -= DT; steps++;
      }
      if (steps === 10) acc = 0;
      if (autopilot && car.needsRescue) { car.rescue(); pilot.sync(); snapPose(); }
    }
    const alpha = acc / DT;
    pos.lerpVectors(prevPos, currPos, alpha); quat.slerpQuaternions(prevQ, currQ, alpha);
    carModel.update(car, pos, quat, sky.night);
    pageMarker.visible = !pageFound();
    if (pageMarker.visible) { pageMarker.getObjectByName('scroll')!.rotation.y += dt * 1.2; pageMarker.getObjectByName('scroll')!.position.y = 1.75 + Math.sin(now / 600) * 0.12; }
    sound.update(dt, { rpm: car.rpm, throttle: paused ? 0 : ctl.throttle, speed: car.speed, onRoad: car.wheels[2].surface === 'road', slip: Math.max(car.wheels[2].slip, car.wheels[3].slip), grounded: !car.airborne, night: sky.night, paused: paused || document.visibilityState !== 'visible' });
    updateCamera(dt);
    labels.update(camera.position);
    sky.update(paused ? 0 : dt, pos, camera);
    shade.update(dt, sky.lightDir, shadeBudget); farShade.update(dt, sky.lightDir, 0.4);
    shade.uniforms.visFloor.value = farShade.uniforms.visFloor.value = sky.night;
    water.tick(now / 1000);
    if (frame % 8 === 0) { terrain.update(camera.position); vegetation.update(camera.position); }
    renderer.info.reset();
    post.render();

    fpsTime += dt; fpsFrames++;
    if (fpsTime >= 0.5) {
      fps = fpsFrames / fpsTime; fpsTime = 0; fpsFrames = 0;
      if (!hud.debug.hidden) updateDebug(fps, 1000 / fps);
      // In Automatic quality, step down when the frame rate stays poor.
      if (settings.quality === 'auto' && !paused && !bench && document.visibilityState === 'visible') {
        slowFor = fps < 40 ? slowFor + 0.5 : 0;
        const i = QUALITIES.indexOf(quality);
        if (slowFor >= 4 && i > 0) { applyQuality(QUALITIES[i - 1]); slowFor = 0; }
      }
    }
    if (!paused && frame % 6 === 0) updateHud();
    if (bannerTimer > 0 && (bannerTimer -= dt) <= 0) hud.banner.classList.remove('show');
    if (bench) benchFrame(rawDt);
  }
  terrain.update(camera.position.copy(currPos)); vegetation.update(camera.position);
  requestAnimationFrame(tick);

  // Hooks for the automated tests and screenshots.
  (window as any).__rihla = {
    ready: true, world, car, sky, renderer, scene,
    start: () => { progress.introSeen = true; sound.start(); openMenu(false); },
    sound, progress, pageMarker,
    setTime: (h: number, scale = 0) => { sky.time = h; sky.timeScale = scale; },
    teleport: (s: number, reverse = false) => { car.placeOnRoad(s, reverse); pilot.sync(); snapPose(); for (let i = 0; i < 4; i++) terrain.update(currPos); vegetation.update(currPos); },
    rebake: () => { sky.update(0, currPos, camera); shade.bakeNow(sky.lightDir); farShade.bakeNow(sky.lightDir); },
    autopilot: (on: boolean) => { autopilot = on; pilot.sync(); },
    camera: (m: typeof camMode) => { camMode = m; camSnap = true; },
    quality: (q: Quality) => applyQuality(q),
    labels: (on: boolean) => { labels.group.visible = on; },
    stats: () => ({ fps, frame, calls: renderer.info.render.calls, triangles: renderer.info.render.triangles, geometries: renderer.info.memory.geometries, textures: renderer.info.memory.textures, speed: car.speed, progress: pilot.progress, y: currPos.y }),
    freeCam: (x: number, y: number, z: number, yaw: number, pitch: number) => { camMode = 'free'; camera.position.set(x, y, z); freeYaw = yaw; freePitch = pitch; },
    bench: startBench,
  };
  if (params.has('autostart')) { progress.introSeen = true; openMenu(false); }
  if (params.has('bench')) startBench();
}

boot().catch((e) => {
  console.error(e);
  $('start').textContent = String(e?.message ?? e);
});
