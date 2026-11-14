// On foot: a child in a long tunic and cap who can leave the car and walk around a place.
// Walking uses Rapier's character controller for walls and buildings, and the exact ground of the world
// (or a raised platform, such as the Haram esplanade) for height.
import RAPIER from '@dimforge/rapier3d-compat';
import * as THREE from 'three';
import { GROUP_WALKER_QUERY, GROUP_WALKER_SELF } from '../physics/groups';
import type { World } from '../world/world';

export interface WalkTop { x: number; z: number; hx: number; hz: number; y: number }
export interface WalkInput { forward: number; turn: number; run: boolean }

const RADIUS = 0.26, HALF = 0.5, WALK = 1.7, RUN = 4.2, TURN = 2.2;

export class Walker {
  readonly pos = new THREE.Vector3();
  readonly model = new THREE.Group();
  yaw = 0;
  speed = 0;
  private phase = 0;
  private readonly body: RAPIER.RigidBody;
  private readonly collider: RAPIER.Collider;
  private readonly controller: RAPIER.KinematicCharacterController;
  private readonly limbs: { legL: THREE.Object3D; legR: THREE.Object3D; armL: THREE.Object3D; armR: THREE.Object3D; body: THREE.Object3D };

  constructor(private readonly rapier: RAPIER.World, private readonly world: World, private readonly tops: WalkTop[], private readonly ignore: () => number | null) {
    this.body = rapier.createRigidBody(RAPIER.RigidBodyDesc.kinematicPositionBased().setTranslation(0, -1000, 0));
    this.collider = rapier.createCollider(RAPIER.ColliderDesc.capsule(HALF, RADIUS).setTranslation(0, HALF + RADIUS + 0.04, 0).setCollisionGroups(GROUP_WALKER_SELF), this.body);
    this.controller = rapier.createCharacterController(0.02);
    this.controller.setSlideEnabled(true);
    this.controller.enableAutostep(0.4, 0.15, false);
    this.controller.setMaxSlopeClimbAngle(1.2);

    // The figure. No face is drawn.
    const skin = new THREE.MeshStandardMaterial({ color: 0xc89a72, roughness: 0.8 });
    const tunic = new THREE.MeshStandardMaterial({ color: 0xece6d6, roughness: 0.9 });
    const vest = new THREE.MeshStandardMaterial({ color: 0x3e5a74, roughness: 0.85 });
    const trousers = new THREE.MeshStandardMaterial({ color: 0x5a5148, roughness: 0.9 });
    const shoe = new THREE.MeshStandardMaterial({ color: 0x2b2420, roughness: 0.7 });
    const cap = new THREE.MeshStandardMaterial({ color: 0xf6f3ea, roughness: 0.9 });
    const part = (g: THREE.BufferGeometry, m: THREE.Material, x: number, y: number, z: number, parent: THREE.Object3D) => { const me = new THREE.Mesh(g, m); me.position.set(x, y, z); me.castShadow = true; parent.add(me); return me; };
    const pivot = (x: number, y: number) => { const p = new THREE.Group(); p.position.set(x, y, 0); this.model.add(p); return p; };
    const legL = pivot(-0.09, 0.62), legR = pivot(0.09, 0.62), armL = pivot(-0.24, 1.12), armR = pivot(0.24, 1.12), body = new THREE.Group();
    this.model.add(body);
    for (const leg of [legL, legR]) { part(new THREE.CapsuleGeometry(0.065, 0.42, 3, 8), trousers, 0, -0.3, 0, leg); part(new THREE.BoxGeometry(0.11, 0.07, 0.22), shoe, 0, -0.6, -0.04, leg); }
    part(new THREE.CylinderGeometry(0.17, 0.25, 0.74, 12), tunic, 0, 0.84, 0, body);
    part(new THREE.CylinderGeometry(0.175, 0.2, 0.36, 12, 1, true), vest, 0, 1.02, 0, body);
    part(new THREE.SphereGeometry(0.115, 14, 10), skin, 0, 1.36, 0, body);
    part(new THREE.SphereGeometry(0.118, 14, 6, 0, Math.PI * 2, 0, 1.2), cap, 0, 1.38, 0, body);
    for (const arm of [armL, armR]) { part(new THREE.CapsuleGeometry(0.05, 0.36, 3, 8), tunic, 0, -0.24, 0, arm); part(new THREE.SphereGeometry(0.05, 8, 6), skin, 0, -0.48, 0, arm); }
    this.limbs = { legL, legR, armL, armR, body };
    this.model.visible = false;
  }

  groundAt(x: number, z: number): number {
    let y = this.world.groundY(x, z);
    for (const t of this.tops) if (Math.abs(x - t.x) < t.hx && Math.abs(z - t.z) < t.hz) y = Math.max(y, t.y);
    return y;
  }

  place(x: number, z: number, yaw: number) {
    this.pos.set(x, this.groundAt(x, z), z); this.yaw = yaw; this.speed = 0;
    this.body.setTranslation(this.pos, true);
  }

  show(on: boolean) { this.model.visible = on; if (!on) this.body.setTranslation({ x: 0, y: -1000, z: 0 }, true); }

  update(dt: number, inp: WalkInput) {
    this.yaw -= inp.turn * TURN * dt;
    const target = inp.forward * (inp.run && inp.forward > 0 ? RUN : WALK);
    this.speed += (target - this.speed) * Math.min(1, dt * 8);
    const dx = -Math.sin(this.yaw) * this.speed * dt, dz = -Math.cos(this.yaw) * this.speed * dt;
    if (Math.abs(this.speed) > 0.01) {
      const skip = this.ignore();
      this.controller.computeColliderMovement(this.collider, { x: dx, y: 0, z: dz }, RAPIER.QueryFilterFlags.EXCLUDE_SENSORS, GROUP_WALKER_QUERY, (c) => c.handle !== skip);
      const m = this.controller.computedMovement();
      const nx = this.pos.x + m.x, nz = this.pos.z + m.z, ny = this.groundAt(nx, nz);
      // Do not walk off a ledge higher than a tall step, or into the sea.
      if (ny - this.pos.y < 0.6 && this.pos.y - ny < 2.5 && ny > this.world.data.waterY + 0.1) this.pos.set(nx, ny, nz);
      else this.speed = 0;
    } else this.pos.y = this.groundAt(this.pos.x, this.pos.z);
    this.body.setTranslation(this.pos, true);

    // Walking animation: swing arms and legs in step, a small bob.
    const pace = Math.abs(this.speed);
    this.phase += dt * (pace < 0.05 ? 0 : 3.2 + pace * 1.6);
    const swing = Math.min(1, pace / WALK) * (pace > WALK + 0.5 ? 0.85 : 0.55), s = Math.sin(this.phase) * swing;
    this.limbs.legL.rotation.x = s; this.limbs.legR.rotation.x = -s;
    this.limbs.armL.rotation.x = -s * 0.9; this.limbs.armR.rotation.x = s * 0.9;
    this.limbs.body.position.y = Math.abs(Math.cos(this.phase)) * 0.03 * Math.min(1, pace);
    this.model.position.copy(this.pos);
    this.model.rotation.set(0, this.yaw, 0);
  }
}
