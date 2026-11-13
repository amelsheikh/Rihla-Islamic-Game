// Keyboard, gamepad and touch input, merged into one set of driving controls.
import type { Controls } from '../physics/vehicle';

export class Input {
  readonly keys = new Set<string>();
  private readonly pressed = new Set<string>();
  private readonly touch = { left: false, right: false, gas: false, brake: false };

  constructor(touchRoot: HTMLElement) {
    addEventListener('keydown', (e) => {
      if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Space'].includes(e.code)) e.preventDefault();
      if (!e.repeat) this.pressed.add(e.code);
      this.keys.add(e.code);
    });
    addEventListener('keyup', (e) => this.keys.delete(e.code));
    addEventListener('blur', () => this.keys.clear());
    for (const el of touchRoot.querySelectorAll<HTMLElement>('[data-touch]')) {
      const k = el.dataset.touch as keyof typeof this.touch;
      const set = (v: boolean) => (e: Event) => { e.preventDefault(); this.touch[k] = v; el.classList.toggle('down', v); };
      el.addEventListener('pointerdown', set(true)); el.addEventListener('pointerup', set(false));
      el.addEventListener('pointercancel', set(false)); el.addEventListener('pointerleave', set(false));
    }
  }

  /** True once for each key press. */
  hit(code: string): boolean { return this.pressed.delete(code); }
  endFrame() { this.pressed.clear(); }

  read(out: Controls): Controls {
    const k = this.keys, d = (a: string, b: string) => (k.has(a) || k.has(b) ? 1 : 0);
    out.throttle = Math.max(d('ArrowUp', 'KeyW'), this.touch.gas ? 1 : 0);
    out.brake = Math.max(d('ArrowDown', 'KeyS'), this.touch.brake ? 1 : 0);
    out.steer = d('ArrowRight', 'KeyD') - d('ArrowLeft', 'KeyA') + (this.touch.right ? 1 : 0) - (this.touch.left ? 1 : 0);
    out.handbrake = k.has('Space');
    const pad = navigator.getGamepads?.().find((p) => p && p.connected);
    if (pad) {
      const ax = pad.axes[0] ?? 0;
      if (Math.abs(ax) > 0.08) out.steer = Math.sign(ax) * Math.abs(ax) ** 1.6;
      out.throttle = Math.max(out.throttle, pad.buttons[7]?.value ?? 0);
      out.brake = Math.max(out.brake, pad.buttons[6]?.value ?? 0);
      out.handbrake ||= !!pad.buttons[0]?.pressed;
    }
    out.steer = Math.max(-1, Math.min(1, out.steer));
    return out;
  }
}
