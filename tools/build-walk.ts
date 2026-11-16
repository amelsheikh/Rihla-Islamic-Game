// Builds the data for the walk in the Old City of Al-Quds (Phase E2) from OpenStreetMap, at true scale.
// Input: tools/data/osm-alquds-east.json, osm-haram.json and osm-trees.json (Overpass API exports; © OpenStreetMap
// contributors, ODbL). Output: unity/Assets/Rihla/Generated/walk.json, in metres from the centre of Bab al-Asbat,
// x east and z north (Unity's axes). Run: npm run walk:build
import { readFileSync, writeFileSync } from 'node:fs';

type Tags = Record<string, string>;
interface OsmNode { type: 'node'; id: number; lat: number; lon: number; tags?: Tags }
interface OsmWay { type: 'way'; id: number; nodes: number[]; tags?: Tags; geometry?: { lat: number; lon: number }[] }
interface OsmRel { type: 'relation'; id: number; tags?: Tags; members: { type: string; ref: number; role: string; geometry?: { lat: number; lon: number }[] }[] }
type El = OsmNode | OsmWay | OsmRel;

const main = JSON.parse(readFileSync('tools/data/osm-alquds-east.json', 'utf8')).elements as El[];
const haram = JSON.parse(readFileSync('tools/data/osm-haram.json', 'utf8')).elements as El[];

const nodes = new Map<number, OsmNode>();
const ways = new Map<number, OsmWay>();
for (const e of main) { if (e.type === 'node') nodes.set(e.id, e); else if (e.type === 'way') ways.set(e.id, e); }

// The origin: the centre of Bab al-Asbat (OSM way 288395731, "Lions' Gate").
const GATE_ID = 288395731;
const gate = ways.get(GATE_ID)!;
const gLat = avg(gate.nodes.map((n) => nodes.get(n)!.lat)), gLon = avg(gate.nodes.map((n) => nodes.get(n)!.lon));
const M_LAT = 110_950, M_LON = 111_320 * Math.cos((gLat * Math.PI) / 180);
const xz = (lat: number, lon: number): [number, number] => [r1((lon - gLon) * M_LON), r1((lat - gLat) * M_LAT)];
const wayXZ = (w: OsmWay) => w.nodes.map((n) => nodes.get(n)).filter(Boolean).map((n) => xz(n!.lat, n!.lon));
function avg(a: number[]) { return a.reduce((s, v) => s + v, 0) / a.length; }
function r1(v: number) { return Math.round(v * 10) / 10; }

// Keep what lies within the walk's area: the gate, the street west of it, and the whole compound.
const AREA = { minX: -520, maxX: 160, minZ: -560, maxZ: 170 };
const inArea = (p: [number, number][]) => p.some(([x, z]) => x >= AREA.minX && x <= AREA.maxX && z >= AREA.minZ && z <= AREA.maxZ);

// Names in the game come from content/, not OSM; these ids mark the buildings the walk is about.
const LANDMARKS: Record<number, string> = {
  4709536: 'dome-of-the-rock', 280309331: 'al-qibli', 45099263: 'dome-of-the-chain',
  288395731: 'bab-al-asbat', 291836688: 'haram-gate-asbat',
};

const buildings: unknown[] = [];
for (const w of ways.values()) {
  const t = w.tags ?? {};
  if (!t.building || w.nodes[0] !== w.nodes[w.nodes.length - 1]) continue;
  if (t.layer && Number(t.layer) < 0) continue;
  const ring = wayXZ(w);
  if (ring.length < 4 || !inArea(ring)) continue;
  const h = t.height ? parseFloat(t.height) : t['building:levels'] ? parseFloat(t['building:levels']) * 3.2 : NaN;
  buildings.push({
    id: w.id, landmark: LANDMARKS[w.id] ?? null, gate: t.historic === 'city_gate', kind: t.building,
    height: Number.isFinite(h) ? h : null, footprint: ring.slice(0, -1),
  });
}

const walls: unknown[] = [];
for (const w of ways.values()) {
  const t = w.tags ?? {};
  if (t.barrier !== 'city_wall' && t.barrier !== 'retaining_wall') continue;
  const line = wayXZ(w);
  if (!inArea(line)) continue;
  walls.push({ id: w.id, kind: t.barrier === 'city_wall' ? 'city' : 'retaining', name: w.id === 137725307 ? 'rock-platform' : null, line });
}

const WIDTH: Record<string, number> = { footway: 2.5, steps: 2.5, path: 2, pedestrian: 5, living_street: 4.5, residential: 5, service: 3.5, tertiary: 7, secondary: 8 };
const streets: unknown[] = [];
for (const w of ways.values()) {
  const t = w.tags ?? {};
  if (!t.highway || t.tunnel === 'yes' || (t.layer && Number(t.layer) < -1)) continue;
  const line = wayXZ(w);
  if (!inArea(line)) continue;
  streets.push({ id: w.id, kind: t.highway, width: WIDTH[t.highway] ?? 3, passage: t.tunnel === 'building_passage', line });
}

// The compound's outline: stitch the outer member ways of OSM relation 5862584 into one ring.
const rel = haram.find((e) => e.type === 'relation') as OsmRel;
const segs = rel.members.filter((m) => m.role === 'outer').map((m) => m.geometry!.map((g) => xz(g.lat, g.lon)));
const ring: [number, number][] = [...segs.shift()!];
const close = (a: [number, number], b: [number, number]) => Math.hypot(a[0] - b[0], a[1] - b[1]) < 0.5;
while (segs.length) {
  const end = ring[ring.length - 1];
  const i = segs.findIndex((s) => close(s[0], end) || close(s[s.length - 1], end));
  if (i < 0) break;
  const s = segs.splice(i, 1)[0];
  ring.push(...(close(s[0], end) ? s : s.slice().reverse()).slice(1));
}
const square = haram.find((e) => e.type === 'way') as OsmWay;

// Trees mapped one by one, and grass and garden areas (tools/data/osm-trees.json).
const greenEls = JSON.parse(readFileSync('tools/data/osm-trees.json', 'utf8')).elements as (OsmNode | OsmWay)[];
const trees: [number, number][] = [];
const greens: [number, number][][] = [];
for (const e of greenEls) {
  if (e.type === 'node') { const p = xz(e.lat, e.lon); if (inArea([p])) trees.push(p); }
  else if (e.geometry && e.geometry.length > 3) {
    const ring = e.geometry.map((g) => xz(g.lat, g.lon));
    if (inArea(ring)) greens.push(ring.slice(0, -1));
  }
}

const centre = (id: number) => { const p = wayXZ(ways.get(id)!).slice(0, -1); return [r1(avg(p.map((v) => v[0]))), r1(avg(p.map((v) => v[1])))]; };
const out = {
  note: 'Generated by tools/build-walk.ts from OpenStreetMap (© OpenStreetMap contributors, ODbL). Metres from the centre of Bab al-Asbat, x east, z north. Heights missing in OSM are filled in Unity as a block-out.',
  origin: { lat: gLat, lon: gLon },
  compound: ring, rockSquare: square.geometry!.map((g) => xz(g.lat, g.lon)),
  points: {
    babAlAsbat: centre(GATE_ID), haramGate: centre(291836688), domeOfTheRock: centre(4709536),
    domeOfTheChain: centre(45099263), alQibli: centre(280309331),
  },
  buildings, walls, streets, trees, greens,
};
writeFileSync('unity/Assets/Rihla/Generated/walk.json', JSON.stringify(out));
console.log(`walk: ${buildings.length} buildings, ${walls.length} walls, ${streets.length} streets, ${trees.length} trees, ${greens.length} green areas, compound ${ring.length} points`);
console.log('points', JSON.stringify(out.points));
