// Drives the car along the road. Used by the tests, the benchmark and (later) "rest while Jiddo drives".
import { Quaternion, Vector3 } from 'three';
import type { World } from '../world/world';
import { CAR, type Controls, type Vehicle } from './vehicle';

export class Autopilot {
  /** Distance along the road, in metres. */
  progress = 0;
  cruise = 15;
  private fwd = new Vector3(); private q = new Quaternion();

  constructor(readonly world: World, readonly car: Vehicle) {}

  sync() {
    const t = this.car.body.translation(), hit = this.world.nearestRoad(t.x, t.z);
    if (hit) this.progress = hit.along;
  }

  update(out: Controls): Controls {
    const w = this.world, t = this.car.body.translation(), r = this.car.body.rotation();
    const hit = w.nearestRoad(t.x, t.z);
    if (hit && Math.abs(hit.along - this.progress) < 60) this.progress = hit.along;
    const speed = Math.max(0, this.car.speed);

    // Slow for the tightest bend in the next stretch.
    let curv = 0;
    for (let d = 4; d < 14 + speed * 2.2; d += 4) {
      const a = w.roadAt(this.progress + d), b = w.roadAt(this.progress + d + 6);
      curv = Math.max(curv, Math.abs(a.dx * b.dz - a.dz * b.dx) / 6);
    }
    const targetSpeed = Math.min(this.cruise, Math.sqrt(3.2 / Math.max(curv, 1e-4)));

    // Pure pursuit: steer towards a point on the road ahead.
    const look = 5 + speed * 0.55, p = w.roadAt(this.progress + look);
    this.q.set(r.x, r.y, r.z, r.w);
    this.fwd.set(0, 0, -1).applyQuaternion(this.q);
    const dx = p.x - t.x, dz = p.z - t.z, dist = Math.hypot(dx, dz);
    const fl = Math.hypot(this.fwd.x, this.fwd.z) || 1;
    const sinEta = (this.fwd.x * dz - this.fwd.z * dx) / (fl * dist); // positive: target is to the right
    const angle = Math.atan((2 * CAR.wheelBase * sinEta) / dist);
    const lock = CAR.maxSteer / (1 + speed / (this.car.mode === 'family' ? 9 : 16));
    out.steer = Math.max(-1, Math.min(1, angle / lock));

    const err = targetSpeed - speed, atEnd = this.progress > w.roadLength - 25;
    out.throttle = atEnd ? 0 : Math.max(0, Math.min(1, err * 0.5));
    out.brake = atEnd ? 1 : Math.max(0, Math.min(1, -err * 0.35 - 0.15));
    out.handbrake = false;
    return out;
  }
}
