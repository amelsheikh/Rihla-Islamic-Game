// Finishing touches on the picture: a soft glow around the sun and lamps, a gentle darkening of the
// corners and a slightly warmer, richer colour. Used on the High and Ultra quality levels only.
import * as THREE from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { ShaderPass } from 'three/addons/postprocessing/ShaderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';

const Grade = {
  uniforms: { tDiffuse: { value: null }, warmth: { value: 0.04 }, saturation: { value: 1.12 }, vignette: { value: 0.32 } },
  vertexShader: 'varying vec2 vUv; void main() { vUv = uv; gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }',
  fragmentShader: `uniform sampler2D tDiffuse; uniform float warmth; uniform float saturation; uniform float vignette; varying vec2 vUv;
    void main() {
      vec4 c = texture2D(tDiffuse, vUv);
      float l = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
      c.rgb = mix(vec3(l), c.rgb, saturation) * vec3(1.0 + warmth, 1.0, 1.0 - warmth);
      vec2 d = vUv - 0.5;
      c.rgb *= 1.0 - vignette * smoothstep(0.25, 0.95, dot(d, d) * 2.0);
      gl_FragColor = c;
    }`,
};

// The sun's disc is so bright it can overflow the glow effect and blank the picture; cap it first.
const Clamp = {
  uniforms: { tDiffuse: { value: null } },
  vertexShader: Grade.vertexShader,
  fragmentShader: `uniform sampler2D tDiffuse; varying vec2 vUv;
    void main() { vec4 c = texture2D(tDiffuse, vUv); c.rgb = clamp(c.rgb, 0.0, 24.0); if (c.r != c.r || c.g != c.g || c.b != c.b) c.rgb = vec3(0.0); gl_FragColor = vec4(c.rgb, 1.0); }`,
};

export class PostFx {
  private composer: EffectComposer | null = null;
  private samples = -1;
  constructor(private renderer: THREE.WebGLRenderer, private scene: THREE.Scene, private camera: THREE.Camera) {}

  /** Turn the effects on (with this many anti-aliasing samples) or off (0 or less). */
  configure(samples: number, width: number, height: number, pixelRatio: number) {
    if (samples <= 0) { this.composer?.dispose(); this.composer = null; this.samples = -1; return; }
    if (samples !== this.samples) {
      this.composer?.dispose();
      const target = new THREE.WebGLRenderTarget(1, 1, { type: THREE.HalfFloatType, samples });
      const c = new EffectComposer(this.renderer, target);
      c.addPass(new RenderPass(this.scene, this.camera));
      c.addPass(new ShaderPass(Clamp));
      c.addPass(new UnrealBloomPass(new THREE.Vector2(256, 256), 0.14, 0.6, 3.2));
      c.addPass(new ShaderPass(Grade));
      c.addPass(new OutputPass());
      this.composer = c; this.samples = samples;
    }
    this.composer!.setPixelRatio(pixelRatio);
    this.composer!.setSize(width, height);
  }

  render() { if (this.composer) this.composer.render(); else this.renderer.render(this.scene, this.camera); }
}
