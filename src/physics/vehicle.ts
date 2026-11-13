// The car: a Rapier rigid body carried by four simulated wheels (spring, damper and a tyre grip curve
// per wheel). Wheels roll on the exact analytic ground from World; the chassis box only collides when
// something has gone wrong (a rollover, a wall). Local axes: +x right, +y up, -z forward.
import RAPIER from '@dimforge/rapier3d-compat';
import { Quaternion, Vector3 } from 'three';
import type { GroundHit, Surface, World } from '../world/world';

export type DriveMode = 'family' | 'simulation';
export interface Controls { throttle: number; brake: number; steer: number; handbrake: boolean }

export const CAR = {
  mass: 1150,
  wheelBase: 2.9,
  track: 1.42,
  wheelRadius: 0.42,
  attachY: -0.05,
  suspRest: 0.32, // spring length at rest under the car's weight
  suspDroop: 0.1, // extra length before the wheel leaves the ground
  suspBump: 0.14,
  damping: 0.5,
  maxSteer: 0.56,
  enginePower: 34_000, // watts; generous for 1910 so that climbs stay fun
  maxDriveForce: 5200,
  topSpeed: 24, // m/s, about 86 km/h
  reverseSpeed: 6,
  brakeForce: 9500,
  dragArea: 1.25,
};

const SURFACE: Record<Surface, { grip: number; rolling: number }> = {
  road: { grip: 0.95, rolling: 0.016 },
  ground: { grip: 0.68, rolling: 0.05 },
};

export interface WheelState { steer: number; spin: number; length: number; grounded: boolean; surface: Surface; slip: number; lx: number; lz: number; prevComp: number }

const K_SPRING = (CAR.mass * 9.81) / 4 / CAR.suspDroop;
const C_DAMP = 2 * CAR.damping * Math.sqrt(K_SPRING * (CAR.mass / 4));

export class Vehicle {
  readonly body: RAPIER.RigidBody;
  readonly wheels: WheelState[];
  mode: DriveMode = 'family';
  /** Signed forward speed in m/s. */
  speed = 0;
  rpm = 800;
  gear = 1;
  steerAngle = 0;
  airborne = false;
  private flippedFor = 0;
  private terrainCollider: RAPIER.Collider | null = null;
  private patchX = Infinity; private patchZ = Infinity;

  // Scratch objects, reused every step.
  private q = new Quaternion(); private pos = new Vector3(); private com = new Vector3(); private lin = new Vector3(); private ang = new Vector3();
  private down = new Vector3(); private a = new Vector3(); private c = new Vector3(); private v = new Vector3(); private f = new Vector3(); private s = new Vector3();
  private nrm = new Vector3(); private force = new Vector3(); private torque = new Vector3(); private tmp = new Vector3(); private fwd = new Vector3(); private up = new Vector3();
  private hit: GroundHit = { y: 0, nx: 0, ny: 1, nz: 0, surface: 'ground' };

  constructor(readonly rapier: RAPIER.World, readonly world: World) {
    this.body = rapier.createRigidBody(RAPIER.RigidBodyDesc.dynamic().setCanSleep(false).setLinearDamping(0).setAngularDamping(0.15).setCcdEnabled(true));
    const m = CAR.mass;
    rapier.createCollider(
      RAPIER.ColliderDesc.cuboid(0.78, 0.3, 1.9)
        .setTranslation(0, 0.2, 0)
        // Centre of mass set low, as on a real chassis with the engine and frame rails down low.
        .setMassProperties(m, { x: 0, y: -0.28, z: 0.05 }, { x: (m / 12) * 15.1, y: (m / 12) * 17, z: (m / 12) * 5.2 }, { w: 1, x: 0, y: 0, z: 0 })
        .setFriction(0.35).setRestitution(0.05),
      this.body,
    );
    const hw = CAR.track / 2, hb = CAR.wheelBase / 2;
    this.wheels = [[-hw, -hb], [hw, -hb], [-hw, hb], [hw, hb]].map(([lx, lz]) => ({ steer: 0, spin: 0, length: CAR.suspRest, grounded: false, surface: 'road' as Surface, slip: 0, lx, lz, prevComp: 0 }));
  }

  /** Put the car on the road at a distance along it, facing down the road. */
  placeOnRoad(s: number, reverse = false) {
    const r = this.world.roadAt(s), sign = reverse ? -1 : 1;
    const yaw = Math.atan2(-r.dx * sign, -r.dz * sign);
    this.q.setFromAxisAngle(new Vector3(0, 1, 0), yaw);
    this.body.setTranslation({ x: r.x, y: r.y + CAR.wheelRadius + CAR.suspRest - CAR.attachY + 0.05, z: r.z }, true);
    this.body.setRotation(this.q, true);
    this.body.setLinvel({ x: 0, y: 0, z: 0 }, true);
    this.body.setAngvel({ x: 0, y: 0, z: 0 }, true);
    for (const w of this.wheels) { w.prevComp = 0; w.length = CAR.suspRest; }
    this.flippedFor = 0;
  }

  /** Called once per physics step, before the Rapier world steps. */
  step(dt: number, ctl: Controls) {
    const body = this.body, family = this.mode === 'family';
    const t = body.translation(), r = body.rotation(), lv = body.linvel(), av = body.angvel(), wc = body.worldCom();
    this.pos.set(t.x, t.y, t.z); this.q.set(r.x, r.y, r.z, r.w); this.lin.set(lv.x, lv.y, lv.z); this.ang.set(av.x, av.y, av.z); this.com.set(wc.x, wc.y, wc.z);
    this.down.set(0, -1, 0).applyQuaternion(this.q);
    this.fwd.set(0, 0, -1).applyQuaternion(this.q);
    this.up.set(0, 1, 0).applyQuaternion(this.q);
    this.speed = this.lin.dot(this.fwd);
    const absSpeed = Math.abs(this.speed);
    this.updateTerrainPatch();

    // Steering: less lock at speed, and a limited turning rate so a tap on a key is not a full swerve.
    const lock = CAR.maxSteer * (family ? 1 / (1 + absSpeed / 9) : 1 / (1 + absSpeed / 16));
    const target = -ctl.steer * lock, rate = (family ? 1.6 : 2.4) * dt;
    this.steerAngle += Math.min(rate, Math.max(-rate, target - this.steerAngle));

    // Drive: power-limited at speed, force-limited from rest. Brake doubles as reverse when stopped.
    let drive = 0, brake = ctl.brake;
    if (ctl.throttle > 0 && this.speed > -0.5) {
      drive = ctl.throttle * Math.min(CAR.maxDriveForce, CAR.enginePower / Math.max(1, absSpeed));
      if (this.speed > CAR.topSpeed) drive = 0;
    } else if (ctl.brake > 0 && this.speed < 0.6 && ctl.throttle === 0) {
      brake = 0;
      drive = this.speed > -CAR.reverseSpeed ? -ctl.brake * CAR.maxDriveForce * 0.6 : 0;
    }
    if (ctl.throttle > 0 && this.speed < -0.5) brake = Math.max(brake, ctl.throttle);
    // Engine braking, and in Family mode a gentle hold on long descents.
    let coast = ctl.throttle === 0 ? 500 + 45 * absSpeed : 0;
    if (family && ctl.throttle === 0 && this.speed > 15) coast += (this.speed - 15) * 700;
    const idleHold = ctl.throttle === 0 && ctl.brake === 0 && absSpeed < 1.2;

    this.force.set(0, 0, 0); this.torque.set(0, 0, 0);
    let grounded = 0;
    for (let i = 0; i < 4; i++) {
      const w = this.wheels[i], front = i < 2;
      w.steer = front ? this.steerAngle : 0;
      this.a.set(w.lx, CAR.attachY, w.lz).applyQuaternion(this.q).add(this.pos);
      const dy = Math.max(0.35, -this.down.y);
      let len = (this.a.y - this.world.groundY(this.a.x, this.a.z)) / dy;
      for (let k = 0; k < 2; k++) {
        this.c.copy(this.a).addScaledVector(this.down, len);
        len += (this.c.y - this.world.groundY(this.c.x, this.c.z)) / dy;
      }
      const maxLen = CAR.suspRest + CAR.suspDroop, minLen = CAR.suspRest - CAR.suspBump;
      const suspLen = len - CAR.wheelRadius;
      w.grounded = suspLen < maxLen && this.down.y < -0.2;
      w.length = Math.min(maxLen, Math.max(minLen, suspLen));
      if (!w.grounded) { w.prevComp = 0; w.slip = 0; w.spin += (this.speed / CAR.wheelRadius) * dt; continue; }
      grounded++;
      this.c.copy(this.a).addScaledVector(this.down, len);
      this.world.ground(this.c.x, this.c.z, this.hit);
      w.surface = this.hit.surface;
      this.nrm.set(this.hit.nx, this.hit.ny, this.hit.nz);

      const comp = maxLen - suspLen, compVel = w.prevComp ? (comp - w.prevComp) / dt : 0;
      w.prevComp = comp;
      let load = K_SPRING * comp + C_DAMP * compVel;
      if (suspLen < minLen) load += K_SPRING * 6 * (minLen - suspLen); // bump stop
      load = Math.min(CAR.mass * 9.81 * 2.5, Math.max(0, load));

      // Velocity of the contact patch, and the wheel's forward and sideways directions on the ground.
      this.tmp.copy(this.c).sub(this.com);
      this.v.copy(this.ang).cross(this.tmp).add(this.lin);
      this.f.set(Math.sin(-w.steer), 0, -Math.cos(w.steer)).applyQuaternion(this.q);
      this.f.addScaledVector(this.nrm, -this.f.dot(this.nrm)).normalize();
      this.s.copy(this.f).cross(this.nrm);
      const vLong = this.v.dot(this.f), vLat = this.v.dot(this.s);

      const surf = SURFACE[w.surface], grip = surf.grip * load, mEff = CAR.mass / 4;
      // Along the wheel: drive on the rear axle, brakes on all four (rear only in Simulation, as in 1910).
      let fx = front ? 0 : drive / 2;
      let stop = surf.rolling * load + (front ? 0 : coast / 2);
      const brakeShare = family ? (front ? 0.3 : 0.2) : front ? 0 : 0.5;
      stop += brake * CAR.brakeForce * brakeShare;
      if ((ctl.handbrake || idleHold) && !front) stop += CAR.brakeForce * 0.5;
      fx -= Math.sign(vLong) * Math.min(stop, (Math.abs(vLong) * mEff) / dt);
      // Across the wheel: a tyre curve at speed, capped by the force that would stop the slide this step.
      const slipAngle = Math.atan2(vLat, Math.abs(vLong) + 0.5);
      const curve = Math.sin(1.45 * Math.atan(7.5 * slipAngle));
      const needed = (Math.abs(vLat) * mEff) / dt;
      let fy = -Math.sign(vLat) * Math.min(Math.abs(curve) * grip, needed * 0.6);
      const total = Math.hypot(fx, fy);
      if (total > grip) { fx *= grip / total; fy *= grip / total; }
      w.slip = Math.min(1, Math.abs(slipAngle) * 2.2 + (total > grip ? 0.5 : 0));
      w.spin += (vLong / CAR.wheelRadius) * dt;

      this.tmp.copy(this.nrm).multiplyScalar(load).addScaledVector(this.f, fx).addScaledVector(this.s, fy);
      this.force.add(this.tmp);
      this.torque.add(this.c.sub(this.com).cross(this.tmp));
    }
    this.airborne = grounded === 0;

    // Air drag.
    const sp = this.lin.length();
    this.force.addScaledVector(this.lin, -0.5 * 1.2 * CAR.dragArea * sp);

    if (family) {
      // Assists: damp spins, and resist rolling over.
      const yawRate = this.ang.dot(this.up);
      this.torque.addScaledVector(this.up, -yawRate * 900 * Math.min(1, absSpeed / 8));
      const roll = Math.asin(Math.max(-1, Math.min(1, this.s.set(1, 0, 0).applyQuaternion(this.q).y)));
      if (Math.abs(roll) > 0.3) this.torque.addScaledVector(this.fwd, -Math.sign(roll) * (Math.abs(roll) - 0.3) * 9000 - this.ang.dot(this.fwd) * 1500);
    }

    body.resetForces(true); body.resetTorques(true);
    body.addForce(this.force, true); body.addTorque(this.torque, true);

    // Engine speed, for the HUD and later the engine sound.
    const ratios = [3.4, 1.9, 1.15], wheelRpm = (absSpeed / CAR.wheelRadius) * 9.55 * 3.9;
    if (this.gear < 3 && wheelRpm * ratios[this.gear - 1] > 2300) this.gear++;
    else if (this.gear > 1 && wheelRpm * ratios[this.gear - 1] < 1000) this.gear--;
    const targetRpm = Math.max(650 + ctl.throttle * 350, wheelRpm * ratios[this.gear - 1]);
    this.rpm += (targetRpm - this.rpm) * Math.min(1, dt * 6);

    // Upside down or on its side and stopped: note it, so the game can offer to right the car.
    this.flippedFor = this.up.y < 0.55 && sp < 3 ? this.flippedFor + dt : 0;
  }

  get needsRescue() { return this.flippedFor > 1 || this.pos.y < this.world.data.waterY - 3; }
  /** How long the car has been lying on its side or roof, in seconds. */
  get tippedFor() { return this.flippedFor; }

  /** Lift the car back onto the nearest stretch of road. */
  rescue() {
    let best = Infinity, bi = 0;
    for (let i = 0; i < this.world.n; i += 4) { const d = (this.world.px[i] - this.pos.x) ** 2 + (this.world.pz[i] - this.pos.z) ** 2; if (d < best) { best = d; bi = i; } }
    this.placeOnRoad(this.world.along[bi], this.fwd.x * (this.world.px[Math.min(this.world.n - 1, bi + 1)] - this.world.px[bi]) + this.fwd.z * (this.world.pz[Math.min(this.world.n - 1, bi + 1)] - this.world.pz[bi]) < 0);
  }

  /** A small terrain collider that follows the car, for the chassis to land on. */
  private updateTerrainPatch() {
    if (Math.hypot(this.pos.x - this.patchX, this.pos.z - this.patchZ) < 24) return;
    const N = 32, cell = 3, size = N * cell;
    const cx = Math.round(this.pos.x / cell) * cell, cz = Math.round(this.pos.z / cell) * cell;
    const heights = new Float32Array((N + 1) * (N + 1));
    // Rapier's layout: index = row + col * (rows + 1), rows along z, columns along x.
    for (let col = 0; col <= N; col++) for (let row = 0; row <= N; row++) heights[row + col * (N + 1)] = this.world.terrainY(cx - size / 2 + col * cell, cz - size / 2 + row * cell);
    if (this.terrainCollider) this.rapier.removeCollider(this.terrainCollider, false);
    this.terrainCollider = this.rapier.createCollider(RAPIER.ColliderDesc.heightfield(N, N, heights, { x: size, y: 1, z: size }).setTranslation(cx, 0, cz).setFriction(0.6));
    this.patchX = cx; this.patchZ = cz;
  }
}
