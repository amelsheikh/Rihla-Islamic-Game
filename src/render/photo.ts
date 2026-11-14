// Photographic materials from Poly Haven (CC0; see ASSETS.md): scanned colour and surface relief.
import * as THREE from 'three';
import ashlarC from '../assets/textures/ashlar_color.jpg';
import ashlarN from '../assets/textures/ashlar_normal.jpg';
import asphaltC from '../assets/textures/asphalt_color.jpg';
import asphaltN from '../assets/textures/asphalt_normal.jpg';
import mudC from '../assets/textures/mud_color.jpg';
import mudN from '../assets/textures/mud_normal.jpg';
import rockC from '../assets/textures/rock_color.jpg';
import rockN from '../assets/textures/rock_normal.jpg';
import soilC from '../assets/textures/soil_color.jpg';
import soilN from '../assets/textures/soil_normal.jpg';

export interface Photo { color: THREE.Texture; normal: THREE.Texture; /** Average colour, linear, for tinting. */ mean: THREE.Vector3 }
export type Photos = Record<'rock' | 'soil' | 'mud' | 'asphalt' | 'ashlar', Photo>;

const lin = (v: number) => Math.pow(v / 255, 2.2);
const SRC: Record<keyof Photos, [string, string, number[]]> = {
  rock: [rockC, rockN, [131, 105, 80]], soil: [soilC, soilN, [168, 143, 105]], mud: [mudC, mudN, [132, 121, 105]],
  asphalt: [asphaltC, asphaltN, [90, 90, 85]], ashlar: [ashlarC, ashlarN, [146, 133, 104]],
};

export async function loadPhotos(): Promise<Photos> {
  const loader = new THREE.TextureLoader(), out = {} as Photos;
  await Promise.all((Object.keys(SRC) as (keyof Photos)[]).map(async (k) => {
    const [c, n, m] = SRC[k], color = await loader.loadAsync(c), normal = await loader.loadAsync(n);
    color.colorSpace = THREE.SRGBColorSpace;
    for (const t of [color, normal]) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; }
    out[k] = { color, normal, mean: new THREE.Vector3(lin(m[0]), lin(m[1]), lin(m[2])) };
  }));
  return out;
}
