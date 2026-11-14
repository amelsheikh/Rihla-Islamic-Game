// All sound is made by the browser's synthesiser: there are no recordings. The car is electric, so it is
// quiet: a soft motor whine that rises with speed, tyre roar that changes with the surface, and wind.
// Around it: birds by day, crickets by night, footsteps on foot, and a horn.
export interface SoundState { rpm: number; throttle: number; speed: number; onRoad: boolean; slip: number; grounded: boolean; night: number; paused: boolean; walking: number }

export class Sound {
  private ctx: AudioContext | null = null;
  private master!: GainNode; private analyser!: AnalyserNode;
  private whineA!: OscillatorNode; private whineB!: OscillatorNode; private whineGain!: GainNode;
  private tyreFilter!: BiquadFilterNode; private tyreGain!: GainNode; private windFilter!: BiquadFilterNode; private windGain!: GainNode;
  private skidGain!: GainNode; private cricketGain!: GainNode; private noiseBuf!: AudioBuffer;
  private nextBird = 3; private stepClock = 0;
  volume = 0.7;

  /** Must be called from a click or key press: browsers only allow sound after one. */
  start() {
    if (this.ctx) { void this.ctx.resume(); return; }
    const Ctor = window.AudioContext ?? (window as any).webkitAudioContext;
    if (!Ctor) return;
    const c = (this.ctx = new Ctor() as AudioContext), now = c.currentTime;
    this.master = c.createGain(); this.master.gain.value = 0;
    const limiter = c.createDynamicsCompressor(); limiter.threshold.value = -12; limiter.ratio.value = 6;
    this.analyser = c.createAnalyser(); this.analyser.fftSize = 1024;
    this.master.connect(limiter).connect(this.analyser).connect(c.destination);

    // Two seconds of soft (brown-ish) noise, reused by every noisy sound.
    const buf = (this.noiseBuf = c.createBuffer(1, c.sampleRate * 2, c.sampleRate)), d = buf.getChannelData(0);
    let last = 0;
    for (let i = 0; i < d.length; i++) { const w = Math.random() * 2 - 1; last = (last + 0.02 * w) / 1.02; d[i] = last * 3.5 + w * 0.08; }
    const noise = () => { const s = c.createBufferSource(); s.buffer = buf; s.loop = true; s.loopStart = Math.random(); s.start(now, Math.random()); return s; };
    const osc = (type: OscillatorType, f: number) => { const o = c.createOscillator(); o.type = type; o.frequency.value = f; o.start(now); return o; };
    const gain = (v: number) => { const g = c.createGain(); g.gain.value = v; return g; };
    const filter = (type: BiquadFilterType, f: number, q = 0.7) => { const b = c.createBiquadFilter(); b.type = type; b.frequency.value = f; b.Q.value = q; return b; };

    // Electric motor: two pure tones a little apart, so they shimmer, kept quiet.
    this.whineGain = gain(0);
    this.whineA = osc('sine', 300); this.whineB = osc('sine', 607);
    this.whineA.connect(gain(0.7)).connect(this.whineGain); this.whineB.connect(gain(0.3)).connect(this.whineGain);
    this.whineGain.connect(this.master);
    // Tyres on the road: low, rounded roar. Wind: a gentle hiss that grows with speed.
    this.tyreFilter = filter('lowpass', 380, 0.5); this.tyreGain = gain(0);
    noise().connect(this.tyreFilter).connect(this.tyreGain).connect(this.master);
    this.windFilter = filter('bandpass', 700, 0.4); this.windGain = gain(0);
    noise().connect(this.windFilter).connect(this.windGain).connect(this.master);
    this.skidGain = gain(0); noise().connect(filter('bandpass', 1400, 3)).connect(this.skidGain).connect(this.master);
    // Crickets: a high tone pulsing quickly, swelling and fading.
    this.cricketGain = gain(0);
    const chirp = gain(0), pulse = osc('square', 31), pulseDepth = gain(0.5); pulse.connect(pulseDepth).connect(chirp.gain);
    const swell = osc('sine', 0.6), swellGain = gain(0.5), swellDepth = gain(0.5); swell.connect(swellDepth).connect(swellGain.gain);
    osc('sine', 4300).connect(chirp).connect(swellGain).connect(this.cricketGain).connect(this.master);
  }

  update(dt: number, s: SoundState) {
    const c = this.ctx;
    if (!c) return;
    const t = c.currentTime, set = (p: AudioParam, v: number, k = 0.08) => p.setTargetAtTime(v, t, k);
    set(this.master.gain, s.paused ? 0 : this.volume, 0.15);
    const v = Math.abs(s.speed), vn = Math.min(1, v / 36), roll = s.grounded ? 1 : 0;
    // The motor's note follows speed; it is loudest while accelerating and nearly silent when coasting.
    set(this.whineA.frequency, 140 + v * 26); set(this.whineB.frequency, 283 + v * 52.6);
    set(this.whineGain.gain, (0.004 + s.throttle * 0.022) * Math.min(1, v / 3 + 0.3) * (1 - s.walking * 0.85));
    set(this.tyreFilter.frequency, (s.onRoad ? 260 : 180) + vn * 520);
    set(this.tyreGain.gain, roll * Math.min(1, v / 4) * (0.05 + vn * 0.16) * (s.onRoad ? 1 : 1.7));
    set(this.windFilter.frequency, 500 + vn * 900);
    set(this.windGain.gain, 0.012 + vn * vn * 0.14);
    set(this.skidGain.gain, roll * Math.max(0, s.slip - 0.5) * 0.18 * Math.min(1, v / 5));
    set(this.cricketGain.gain, s.night * 0.018, 0.5);
    this.nextBird -= dt;
    if (this.nextBird <= 0 && !s.paused) { this.nextBird = 2.5 + Math.random() * 6; if (s.night < 0.3) this.bird(); }
  }

  /** A footstep on stony ground, called by the game in time with the walker's stride. */
  step(run: boolean) {
    const c = this.ctx;
    if (!c) return;
    const t = c.currentTime, src = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
    src.buffer = this.noiseBuf; f.type = 'bandpass'; f.frequency.value = 900 + Math.random() * 500; f.Q.value = 1.2;
    g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(run ? 0.22 : 0.14, t + 0.008); g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
    src.connect(f).connect(g).connect(this.master); src.start(t, Math.random() * 1.5); src.stop(t + 0.14);
  }

  private bird() {
    const c = this.ctx!, pan = c.createStereoPanner(); pan.pan.value = Math.random() * 2 - 1; pan.connect(this.master);
    const base = 2200 + Math.random() * 1600, notes = 2 + Math.floor(Math.random() * 4);
    for (let i = 0; i < notes; i++) {
      const t = c.currentTime + i * (0.11 + Math.random() * 0.05), o = c.createOscillator(), g = c.createGain();
      o.frequency.setValueAtTime(base * (0.9 + Math.random() * 0.3), t); o.frequency.exponentialRampToValueAtTime(base * (1.15 + Math.random() * 0.3), t + 0.05); o.frequency.exponentialRampToValueAtTime(base, t + 0.09);
      g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(0.03, t + 0.015); g.gain.linearRampToValueAtTime(0, t + 0.09);
      o.connect(g).connect(pan); o.start(t); o.stop(t + 0.1);
    }
  }

  /** A modern car horn: two notes together, a short double press. */
  horn() {
    const c = this.ctx;
    if (!c) return;
    for (const start of [0, 0.28]) {
      const t = c.currentTime + start, g = c.createGain(), f = c.createBiquadFilter();
      f.type = 'lowpass'; f.frequency.value = 2400; g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(0.16, t + 0.02); g.gain.setValueAtTime(0.16, t + 0.17); g.gain.linearRampToValueAtTime(0, t + 0.22);
      for (const hz of [415, 505]) { const o = c.createOscillator(); o.type = 'square'; o.frequency.value = hz; o.connect(f); o.start(t); o.stop(t + 0.24); }
      f.connect(g).connect(this.master);
    }
  }

  /** Loudness right now (0..1), for the automated tests. */
  level(): number {
    if (!this.ctx) return 0;
    const d = new Float32Array(this.analyser.fftSize); this.analyser.getFloatTimeDomainData(d);
    let s = 0, peak = 0; for (const v of d) { s += v * v; peak = Math.max(peak, Math.abs(v)); }
    return Math.max(Math.sqrt(s / d.length), peak > 0.99 ? 1 : 0);
  }
}
