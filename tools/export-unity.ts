// Exports the browser game's terrain, road and places for the Unity project, and copies content/ into it.
// Run with `npm run unity:export` after `npm run terrain`. Reads src/generated/, writes unity/Assets/Rihla/.
//
// Axes: the browser game uses +x east, -z north, +y up (metres). Unity uses +x east, +z north, +y up,
// so every point becomes (x, y, -z). Heights are the game's (already compressed by V_SCALE).
//
// Terrain: Unity terrains need a square heightmap of 2^n + 1 samples. The near grid (3 m) is cut into
// 513 x 513 tiles of 1536 m; the far grid (36 m) becomes one 13.8 km terrain with a hole where the near
// tiles are. Each heightmap is raw 16-bit little-endian, rows from south to north, scaled to the shared
// height range in terrain.json.
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

const ROOT = path.resolve(import.meta.dirname, '..');
const GEN = path.join(ROOT, 'src/generated');
const OUT = path.join(ROOT, 'unity/Assets/Rihla/Generated');
const CONTENT_OUT = path.join(ROOT, 'unity/Assets/Rihla/Content');

interface Grid { cell: number; cols: number; rows: number; x0: number; z0: number; yMin: number; yMax: number; file: string }
const world = JSON.parse(fs.readFileSync(path.join(GEN, 'world.json'), 'utf8'));

function decode(g: Grid) {
  const raw = zlib.gunzipSync(fs.readFileSync(path.join(GEN, g.file)));
  const w = g.cols + 1, n = w * (g.rows + 1), k = (g.yMax - g.yMin) / 65535, out = new Float32Array(n);
  for (let r = 0; r <= g.rows; r++) {
    let q = 0;
    for (let c = 0; c < w; c++) { q = (q + raw.readUInt16LE((r * w + c) * 2)) & 0xffff; out[r * w + c] = g.yMin + q * k; }
  }
  return out;
}
function sampler(g: Grid, d: Float32Array) {
  const w = g.cols + 1;
  return {
    inside: (x: number, z: number) => x >= g.x0 && z >= g.z0 && x <= g.x0 + g.cols * g.cell && z <= g.z0 + g.rows * g.cell,
    at(x: number, z: number) {
      const gx = Math.min(g.cols - 1e-4, Math.max(0, (x - g.x0) / g.cell)), gz = Math.min(g.rows - 1e-4, Math.max(0, (z - g.z0) / g.cell));
      const ix = gx | 0, iz = gz | 0, fx = gx - ix, fz = gz - iz, i = iz * w + ix;
      return (d[i] * (1 - fx) + d[i + 1] * fx) * (1 - fz) + (d[i + w] * (1 - fx) + d[i + w + 1] * fx) * fz;
    },
  };
}

const nearG: Grid = world.near, farG: Grid = world.far;
const near = sampler(nearG, decode(nearG)), far = sampler(farG, decode(farG));
/** Height at a game-space point: the near grid where it exists, the far grid elsewhere. */
const heightGame = (x: number, z: number) => (near.inside(x, z) ? near.at(x, z) : far.at(x, z));

const Y_MIN = Math.floor(Math.min(nearG.yMin, farG.yMin)) - 2, Y_MAX = Math.ceil(Math.max(nearG.yMax, farG.yMax)) + 2;
const quant = (y: number) => Math.round(Math.min(1, Math.max(0, (y - Y_MIN) / (Y_MAX - Y_MIN))) * 65535);

fs.rmSync(OUT, { recursive: true, force: true });
fs.mkdirSync(OUT, { recursive: true });

const RES = 513;
/** Writes one terrain whose south-west corner is at Unity (ux0, uz0), `size` metres square. */
function writeTerrain(name: string, ux0: number, uz0: number, size: number, sample: (ux: number, uz: number) => number) {
  const buf = Buffer.alloc(RES * RES * 2), step = size / (RES - 1);
  for (let r = 0; r < RES; r++) for (let c = 0; c < RES; c++) buf.writeUInt16LE(quant(sample(ux0 + c * step, uz0 + r * step)), (r * RES + c) * 2);
  fs.writeFileSync(path.join(OUT, `${name}.bytes`), buf);
  return { name, file: `${name}.bytes`, resolution: RES, position: [ux0, Y_MIN, uz0], size: [size, Y_MAX - Y_MIN, size] };
}

const terrains: any[] = [];
// Near tiles. The near grid's game z range [z0, z0 + rows*cell] is Unity z [-(z0 + rows*cell), -z0].
const TILE = (RES - 1) * nearG.cell;
const nx = Math.ceil(nearG.cols * nearG.cell / TILE), nz = Math.ceil(nearG.rows * nearG.cell / TILE);
const nearUx0 = nearG.x0, nearUz0 = -(nearG.z0 + nearG.rows * nearG.cell);
for (let j = 0; j < nz; j++) for (let i = 0; i < nx; i++) {
  const t = writeTerrain(`near_${i}_${j}`, nearUx0 + i * TILE, nearUz0 + j * TILE, TILE, (ux, uz) => heightGame(ux, -uz));
  terrains.push({ ...t, kind: 'near', tile: [i, j] });
}
const nearRect = { x0: nearUx0, z0: nearUz0, x1: nearUx0 + nx * TILE, z1: nearUz0 + nz * TILE };

// Far terrain, square, covering the far grid, with a hole under the near tiles.
const FAR_SIZE = Math.max(farG.cols, farG.rows) * farG.cell;
const farUx0 = farG.x0, farUz0 = -(farG.z0 + farG.rows * farG.cell);
const FAR_DROP = 1.5; // sits slightly below the near tiles where the two overlap at the hole's edge
const farT = writeTerrain('far', farUx0, farUz0, FAR_SIZE, (ux, uz) => far.at(ux, -uz) - FAR_DROP);
{
  // Holes: one byte per heightmap cell (RES-1 squared), 1 = solid, 0 = hole. Rows south to north.
  const n = RES - 1, cell = FAR_SIZE / n, holes = Buffer.alloc(n * n, 1);
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) {
    const x0 = farUx0 + c * cell, z0 = farUz0 + r * cell;
    if (x0 >= nearRect.x0 && x0 + cell <= nearRect.x1 && z0 >= nearRect.z0 && z0 + cell <= nearRect.z1) holes[r * n + c] = 0;
  }
  fs.writeFileSync(path.join(OUT, 'far_holes.bytes'), holes);
  terrains.push({ ...farT, kind: 'far', holes: 'far_holes.bytes' });
}

const toUnity = (x: number, y: number, z: number) => [x, y, -z].map((v) => Math.round(v * 100) / 100);
const pts: number[] = world.road.points, road: number[][] = [];
for (let i = 0; i < pts.length; i += 3) road.push(toUnity(pts[i], pts[i + 1], pts[i + 2]));

const scene = {
  generated: 'by tools/export-unity.ts from src/generated/world.json; do not edit by hand',
  axes: 'Unity: +x east, +y up, +z north, metres; heights compressed as in the browser game (see src/world/warp.ts)',
  waterY: world.waterY,
  heightRange: [Y_MIN, Y_MAX],
  terrains,
  road: { id: world.road.id, name: world.road.name, width: world.road.width, length: world.road.length, seaLevelAt: world.road.seaLevelAt, points: road },
  places: world.places.map((p: any) => ({ id: p.id, kind: p.kind, name: p.name, position: toUnity(p.x, p.y, p.z), roadDist: p.roadDist, offRoad: p.offRoad })),
  walls: world.walls.map(([x, z]: number[]) => [x, -z]),
};
// Unity reads .json files in Assets as TextAssets.
fs.writeFileSync(path.join(OUT, 'scene.json'), JSON.stringify(scene, null, 1));

// Content: copied unchanged; content/ in the repository stays the one source to edit.
fs.rmSync(CONTENT_OUT, { recursive: true, force: true });
fs.cpSync(path.join(ROOT, 'content'), CONTENT_OUT, { recursive: true });
fs.writeFileSync(path.join(CONTENT_OUT, 'README.txt'),
  'Copied from content/ by tools/export-unity.ts. Do not edit here: edit content/ and run npm run unity:export.\n');

console.log(`terrain: ${nx}x${nz} near tiles of ${TILE} m, far ${FAR_SIZE} m; heights ${Y_MIN}..${Y_MAX}`);
console.log(`road: ${road.length} points, ${world.road.length} m; places: ${scene.places.length}; content copied`);
