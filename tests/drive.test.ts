// Drives the whole road with the autopilot, without graphics, and checks the car arrives in one piece.
import fs from 'node:fs';
import path from 'node:path';
import RAPIER from '@dimforge/rapier3d-compat';
import { describe, expect, it } from 'vitest';
import { Autopilot } from '../src/physics/autopilot';
import { Vehicle, type Controls, type DriveMode } from '../src/physics/vehicle';
import { Heightmap } from '../src/world/heightmap';
import { toGame, toReal } from '../src/world/warp';
import { World, type WorldData } from '../src/world/world';

const gen = path.resolve(import.meta.dirname, '../src/generated');
const data: WorldData = JSON.parse(fs.readFileSync(path.join(gen, 'world.json'), 'utf8'));

async function loadWorld() {
  await RAPIER.init();
  const near = await Heightmap.decode(data.near, fs.readFileSync(path.join(gen, data.near.file)));
  const far = await Heightmap.decode(data.far, fs.readFileSync(path.join(gen, data.far.file)));
  return new World(data, near, far);
}

describe('warp', () => {
  it('maps real to game and back', () => {
    for (const [lon, lat] of [[35.2345, 31.778], [35.24, 31.775], [35.3, 31.8], [35.459, 31.856], [35.47, 31.84], [35.6, 31.6]]) {
      const [x, z] = toGame(lon, lat), [lon2, lat2] = toReal(x, z);
      expect(lon2).toBeCloseTo(lon, 6);
      expect(lat2).toBeCloseTo(lat, 6);
    }
  });
});

describe('the Jericho road', () => {
  it('descends from Al-Quds to below sea level', async () => {
    const w = await loadWorld();
    expect(w.py[0] * 2.5).toBeGreaterThan(650);
    expect(w.py[w.n - 1] * 2.5).toBeLessThan(-380);
    expect(data.road.seaLevelAt).toBeGreaterThan(0);
  });

  for (const mode of ['family', 'simulation'] as DriveMode[]) {
    it(`can be driven end to end by the autopilot in ${mode} mode`, async () => {
      const world = await loadWorld();
      const rapier = new RAPIER.World({ x: 0, y: -9.81, z: 0 });
      const dt = 1 / 120;
      rapier.timestep = dt;
      const car = new Vehicle(rapier, world);
      car.mode = mode;
      car.placeOnRoad(5);
      const pilot = new Autopilot(world, car);
      const ctl: Controls = { throttle: 0, brake: 0, steer: 0, handbrake: false };
      let time = 0, maxSpeed = 0, maxOff = 0, rescues = 0, minUp = 1;
      while (pilot.progress < world.roadLength - 30 && time < 1500) {
        pilot.update(ctl);
        car.step(dt, ctl);
        rapier.step();
        time += dt;
        maxSpeed = Math.max(maxSpeed, car.speed);
        const t = car.body.translation(), r = car.body.rotation();
        const hit = world.nearestRoad(t.x, t.z);
        maxOff = Math.max(maxOff, hit ? hit.dist : 99);
        minUp = Math.min(minUp, 1 - 2 * (r.x * r.x + r.z * r.z));
        if (car.needsRescue) { rescues++; car.rescue(); pilot.sync(); }
      }
      console.log(`${mode}: ${(world.roadLength / 1000).toFixed(2)} km in ${(time / 60).toFixed(1)} min, mean ${((pilot.progress / time) * 3.6).toFixed(0)} km/h, ` +
        `max ${(maxSpeed * 3.6).toFixed(0)} km/h, furthest from centre ${maxOff.toFixed(1)} m, most tilt ${((Math.acos(minUp) * 180) / Math.PI).toFixed(0)} deg, rescues ${rescues}`);
      expect(pilot.progress).toBeGreaterThan(world.roadLength - 31);
      expect(rescues).toBe(0);
      expect(maxOff).toBeLessThan(world.halfWidth);
    });
  }

  it('lets the car rest on the road without sliding or sinking', async () => {
    const world = await loadWorld();
    const rapier = new RAPIER.World({ x: 0, y: -9.81, z: 0 });
    rapier.timestep = 1 / 120;
    const car = new Vehicle(rapier, world);
    car.placeOnRoad(3000);
    const start = { ...car.body.translation() };
    const ctl: Controls = { throttle: 0, brake: 0, steer: 0, handbrake: false };
    for (let i = 0; i < 120 * 6; i++) { car.step(1 / 120, ctl); rapier.step(); }
    const end = car.body.translation();
    expect(Math.hypot(end.x - start.x, end.z - start.z)).toBeLessThan(0.3);
    expect(Math.abs(end.y - start.y)).toBeLessThan(0.25);
  });
});
