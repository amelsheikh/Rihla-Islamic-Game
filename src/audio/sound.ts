// All sound is made by the browser's synthesiser: there are no recordings. A quiet modern four-cylinder
// engine, tyres, wind, birds by day, crickets by night and a horn.
export interface SoundState { rpm: number; throttle: number; speed: number; onRoad: boolean; slip: number; grounded: boolean; night: number; paused: boolean }

export class Sound {
  private ctx: AudioContext | null = null;
  private master!: GainNode; private analyser!: AnalyserNode;
  private engA!: OscillatorNode; private engB!: OscillatorNode; private engC!: OscillatorNode; private chuffLfo!: OscillatorNode;
  private engFilter!: BiquadFilterNode; private engGain!: GainNode; private chuffGain!: GainNode;
  private tyreFilter!: BiquadFilterNode; private tyreGain!: GainNode; private windGain!: GainNode; private skidGain!: GainNode; private cricketGain!: GainNode;
  private nextBird = 3;
  volume = 0.7;

  /** Must be called from a click or key press: browsers only allow sound after one. */
  start() {
    if (this.ctx) { void this.ctx.resume(); return; }
    const Ctor = window.AudioContext ?? (window as any).webkitAudioContext;
    if (!Ctor) return;
    const c = (this.ctx = new Ctor() as AudioContext), now = c.currentTime;
    this.master = c.createGain(); this.master.gain.value = 0;
    const limiter = c.createDynamicsCompressor(); limiter.threshold.value = -10; limiter.ratio.value = 8;
    this.analyser = c.createAnalyser(); this.analyser.fftSize = 1024;
    this.master.connect(limiter).connect(this.analyser).connect(c.destination);

    // Two seconds of noise, reused by every noisy sound.
    const buf = c.createBuffer(1, c.sampleRate * 2, c.sampleRate), d = buf.getChannelData(0);
    let last = 0;
    for (let i = 0; i < d.length; i++) { const w = Math.random() * 2 - 1; last = (last + 0.04 * w) / 1.04; d[i] = last * 3 + w * 0.25; }
    const noise = () => { const s = c.createBufferSource(); s.buffer = buf; s.loop = true; s.start(now); return s; };
    const osc = (type: OscillatorType, f: number) => { const o = c.createOscillator(); o.type = type; o.frequency.value = f; o.start(now); return o; };
    const gain = (v: number) => { const g = c.createGain(); g.gain.value = v; return g; };
    const filter = (type: BiquadFilterType, f: number, q = 1) => { const b = c.createBiquadFilter(); b.type = type; b.frequency.value = f; b.Q.value = q; return b; };

    // Engine: the firing note, a rough sub-note an octave down and a light overtone, through a low filter.
    this.engA = osc('sawtooth', 27); this.engB = osc('triangle', 13.5); this.engC = osc('triangle', 54);
    this.engFilter = filter('lowpass', 320, 1.2); this.engGain = gain(0.2);
    this.engA.connect(gain(0.5)).connect(this.engFilter); this.engB.connect(gain(0.3)).connect(this.engFilter); this.engC.connect(gain(0.22)).connect(this.engFilter);
    this.engFilter.connect(this.engGain).connect(this.master);
    // The "chuff" of the exhaust: noise switched on and off at the firing rate.
    this.chuffGain = gain(0); this.chuffLfo = osc('square', 25);
    const chuffDepth = gain(0.5); this.chuffLfo.connect(chuffDepth).connect(this.chuffGain.gain);
    const chuffOut = gain(0.04);
    noise().connect(filter('bandpass', 260, 0.8)).connect(this.chuffGain).connect(chuffOut).connect(this.master);

    this.tyreFilter = filter('bandpass', 420, 0.6); this.tyreGain = gain(0);
    noise().connect(this.tyreFilter).connect(this.tyreGain).connect(this.master);
    this.windGain = gain(0); noise().connect(filter('lowpass', 520, 0.3)).connect(this.windGain).connect(this.master);
    this.skidGain = gain(0); noise().connect(filter('bandpass', 1700, 4)).connect(this.skidGain).connect(this.master);
    // Crickets: a high tone, pulsing quickly, swelling and fading.
    this.cricketGain = gain(0);
    const chirp = gain(0), pulse = osc('square', 31), pulseDepth = gain(0.5); pulse.connect(pulseDepth).connect(chirp.gain);
    const swell = osc('sine', 0.6), swellGain = gain(0.5), swellDepth = gain(0.5); swell.connect(swellDepth).connect(swellGain.gain);
    osc('sine', 4300).connect(chirp).connect(swellGain).connect(this.cricketGain).connect(this.master);
  }

  update(dt: number, s: SoundState) {
    const c = this.ctx;
    if (!c) return;
    const t = c.currentTime, set = (p: AudioParam, v: number, k = 0.06) => p.setTargetAtTime(v, t, k);
    set(this.master.gain, s.paused ? 0 : this.volume, 0.15);
    const fire = (s.rpm / 60) * 2; // two firings per turn of the crankshaft
    set(this.engA.frequency, fire); set(this.engB.frequency, fire / 2); set(this.engC.frequency, fire * 2); set(this.chuffLfo.frequency, fire);
    set(this.engFilter.frequency, 220 + s.throttle * 700 + s.rpm * 0.22);
    set(this.engGain.gain, 0.07 + s.throttle * 0.15);
    set(this.chuffGain.gain, 0.35 + s.throttle * 0.4);
    const v = Math.min(1, Math.abs(s.speed) / 36), roll = s.grounded ? 1 : 0;
    set(this.tyreFilter.frequency, s.onRoad ? 520 : 260);
    set(this.tyreGain.gain, roll * v * (s.onRoad ? 0.16 : 0.34));
    set(this.windGain.gain, 0.02 + v * v * 0.22);
    set(this.skidGain.gain, roll * Math.max(0, s.slip - 0.45) * 0.25 * Math.min(1, Math.abs(s.speed) / 5));
    set(this.cricketGain.gain, s.night * 0.02, 0.5);
    // Birdsong by day, in short bursts.
    this.nextBird -= dt;
    if (this.nextBird <= 0 && !s.paused) {
      this.nextBird = 2.5 + Math.random() * 6;
      if (s.night < 0.3) this.bird();
    }
  }

  private bird() {
    const c = this.ctx!, pan = c.createStereoPanner(); pan.pan.value = Math.random() * 2 - 1; pan.connect(this.master);
    const base = 2200 + Math.random() * 1600, notes = 2 + Math.floor(Math.random() * 4);
    for (let i = 0; i < notes; i++) {
      const t = c.currentTime + i * (0.11 + Math.random() * 0.05), o = c.createOscillator(), g = c.createGain();
      o.frequency.setValueAtTime(base * (0.9 + Math.random() * 0.3), t); o.frequency.exponentialRampToValueAtTime(base * (1.15 + Math.random() * 0.3), t + 0.05); o.frequency.exponentialRampToValueAtTime(base, t + 0.09);
      g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(0.035, t + 0.015); g.gain.linearRampToValueAtTime(0, t + 0.09);
      o.connect(g).connect(pan); o.start(t); o.stop(t + 0.1);
    }
  }

  /** A bulb horn: two quick honks. */
  horn() {
    const c = this.ctx;
    if (!c) return;
    for (const start of [0, 0.32]) {
      const t = c.currentTime + start, o = c.createOscillator(), o2 = c.createOscillator(), g = c.createGain(), f = c.createBiquadFilter();
      o.type = 'sawtooth'; o2.type = 'square'; f.type = 'bandpass'; f.frequency.value = 900; f.Q.value = 1.5;
      for (const [osc, m] of [[o, 1], [o2, 1.5]] as const) { osc.frequency.setValueAtTime(190 * m, t); osc.frequency.exponentialRampToValueAtTime(330 * m, t + 0.07); osc.frequency.setValueAtTime(330 * m, t + 0.18); osc.frequency.exponentialRampToValueAtTime(250 * m, t + 0.25); }
      g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(0.3, t + 0.03); g.gain.setValueAtTime(0.3, t + 0.19); g.gain.linearRampToValueAtTime(0, t + 0.26);
      o.connect(f); o2.connect(f); f.connect(g).connect(this.master); o.start(t); o2.start(t); o.stop(t + 0.3); o2.stop(t + 0.3);
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
