# Rihla: Roads Without Borders — Technical Design

Status: approved by the owner. Phase 1a is built. This file is kept up to date as decisions change.
Section 9 lists what Phase 1a actually built and where it differs from this plan.

## 1. Stack

| Area | Choice | Why |
|---|---|---|
| Language and build | TypeScript (strict) + Vite | As the brief asks. |
| Renderer | Three.js `WebGLRenderer` (WebGL2), version pinned at the start of Phase 1 | The `postprocessing` library only works with the WebGL renderer. Custom shaders are kept in a small number of files so a later WebGPU port touches few places. |
| Post-processing | `postprocessing`: AgX tone mapping, bloom, GTAO, per-region colour LUT, vignette; depth of field in photo mode only | One merged effect pass keeps the cost low. GTAO is off on the Low preset. |
| Physics | Rapier (`@dimforge/rapier3d-compat`), fixed 60 Hz step with render interpolation | |
| Vehicle | **Custom** raycast vehicle on a Rapier rigid body, not Rapier's built-in vehicle controller | The built-in controller has a very simple tyre model. A custom one gives per-surface grip curves, weight transfer and the assist layer the three driving modes need. |
| Audio | WebAudio directly, with a small wrapper | The engine sound needs RPM- and load-driven synthesis and crossfading, which Howler does not do. Howler is not needed on top. |
| Tests | Vitest for logic, Playwright for play-tests and screenshots | |
| Saves | IndexedDB, 3 slots, versioned schema | |

## 2. World scale: the main design decision

Real distances are too long, and squeezing the map sideways while keeping real heights makes every hill a cliff
(Al-Quds to Ariha would average a 24% slope). The proposal:

- **Horizontal compression about 1:8** in Palestine, Lebanon and western Transjordan. Neighbouring cities are
  then 3–6 minutes apart at 1910 touring-car speeds (60–70 km/h).
- **Vertical compression about 1:2.5.** The descent from Al-Quds to the Dead Sea becomes roughly 480 m of real
  in-game drop over about 5 km: steep, dramatic switchbacks, but drivable.
- **The HUD altimeter always shows the true elevation** (for example "−430 m" at the Dead Sea), and a roadside
  "sea level" marker appears on the descent, so the geography lesson stays accurate.
- **Cities and landmarks are compressed less** than the open country between them, so they have room for streets.
- **Long empty stretches are compressed more** (Sinai, the eastern desert: about 1:20), with an optional
  "rest while Jiddo drives" skip, so nobody has to drive 35 minutes of flat desert.
- A floating origin keeps physics and rendering precise across a world roughly 80 km long.

## 3. Content pipeline (offline, run with Node)

1. Download public elevation data (Copernicus 30 m DEM / Terrarium tiles; both are reachable from this
   environment) for the region being built.
2. Warp it to game space using the compression above and a table of anchor points (cities, landmarks).
3. Flatten and bank the terrain along road splines, so the visible road and the physics surface always agree.
4. Write out 16-bit height tiles, splat maps (rock, soil, sand, scrub, field, terrace), vegetation density maps
   and a low-resolution far-distance mesh.
5. Validate all content JSON against schemas; the build fails on a broken reference or a missing translation.

Roads, places, quests, dialogue, quizzes, religious texts and localisation strings are all data files under
`content/`. Road splines are authored as latitude/longitude points along the historical routes.

## 4. Rendering

- **Terrain:** streamed square chunks with 4 levels of detail and skirts; splat-map texturing with triplanar
  mapping on steep slopes; a distant low-detail ring out to the horizon so the Jordan Valley and the mountains
  of Moab are visible from the Mount of Olives.
- **Sky and light:** analytic atmospheric-scattering sky; the sun and moon move on a real path for the latitude
  and date; one shadowed directional light with 3 cascades; ambient light from the sky, re-captured in slices
  every few game-minutes; stars and crescent moon at night; height fog and valley haze; heat shimmer as a
  screen-space effect in deserts.
- **Vegetation:** instanced meshes per chunk, placed from density maps; wind in the vertex shader; billboards at
  distance. Trees (olive, cypress, date palm, cedar, orange) come from a **procedural tree generator**, because
  good CC0 models of these species are scarce.
- **Landmarks:** a parametric architecture kit (pointed and horseshoe arches, domes on drums, minarets by
  period style, ablaq stone courses, crenellated walls, mashrabiya, tile bands) driven by JSON, so a landmark
  is data plus a few custom parts. Proportions come from measured references, recorded in `ASSETS.md`.
- **Water:** the Dead Sea, the Mediterranean and Lake Tiberias use one water shader with depth-based colour,
  reflections of the sky and shoreline foam.
- **Cameras:** chase, bonnet, interior, arrival drone shot, photo mode.

### Budgets (High preset, mid-range laptop)

| Item | Budget |
|---|---|
| Draw calls | under 350 |
| Triangles on screen | under 1.5 million |
| GPU texture memory | under 600 MB |
| Main-thread frame time | under 8 ms, leaving the rest for the GPU |
| First download for the vertical slice | under 40 MB, then streamed |

Low and Medium presets cut shadow cascades, vegetation density, render scale and GTAO for tablets.

## 5. Code layout

```
src/core      loop, input (keyboard, gamepad, touch), settings, events
src/render    sky, terrain, vegetation, water, landmarks, post-fx, cameras
src/physics   vehicle, surfaces, assists
src/world     streaming, regions, roads, time of day, weather
src/game      quests, deliveries, kindness events, quizzes, progression
src/visit     on-foot visit mode for sacred sites
src/ui        HUD, journal, map, menus (HTML/CSS over the canvas, RTL-aware)
src/audio     engine synth, ambience, narration, adhan scheduling
src/save      IndexedDB slots
content/      all data: places, roads, quests, dialogue, quizzes,
              religious_texts.json, i18n/ar.json, i18n/en.json
tools/        the offline pipeline in section 3
tests/        unit tests and Playwright play-tests
```

The UI is HTML and CSS over the canvas, not drawn in WebGL. That gives correct Arabic shaping, right-to-left
layout and accessibility for free.

## 6. Phases

| Phase | Result |
|---|---|
| 1a — Foundation | Grey-box build: real terrain of the Al-Quds to Ariha corridor, the road, one car with proper physics, the sky and day-night cycle, quality presets, the benchmark page. Playable, not yet pretty. |
| 1b — Vertical slice | The same 10 minutes made beautiful: vegetation, materials, Al-Quds skyline and walls, the khan, one journal page, sound, Family and Simulation modes, Arabic and English UI. |
| 2 — Core systems | Visit mode, adhan and prayer stops, journal, quizzes, deliveries, kindness events, saves, gamepad and touch, photo mode. |
| 3 — Palestine | All listed cities and landmarks, weather, the chapter story. |
| 4 / 5 / 6 | Transjordan, Lebanon, Egypt and Sinai. |
| 7 — Polish | Performance, accessibility, localisation check, tutorial, installable build. |

Splitting Phase 1 in two gives the owner something to drive early, before the art work is spent on
foundations that might still change.

## 7. Risks

| Risk | Mitigation |
|---|---|
| **Frame rates cannot be measured here.** This environment has no graphics card; the browser renders in software. Screenshots are accurate, FPS is not. | Report draw calls, triangle counts and CPU frame time from here, and ship a one-click **benchmark page** the owner runs on their own laptop and tablet; it shows a result to paste back. |
| The look falls short of "wow". Browser graphics and code-built models have a ceiling. | Spend the effort where it shows most: light, atmosphere, terrain silhouette, vegetation density. Hero landmarks are designed so commissioned 3D models can replace them later without code changes. |
| Poly Haven, ambientCG and Kenney are **blocked by this environment's network policy**. | Textures and trees are generated procedurally, which is also lighter to download. If the owner wants those libraries, the hosts can be added to the environment's allowed list. |
| Landmark accuracy: code-built architecture can look generic or wrong. | Build from measured proportions, compare screenshots with period photographs, and list anything uncertain in `NEEDS_REVIEW.md`. |
| Historical or religious error. | All such text lives in data files with sources; nothing uncertain ships without review. |
| Adhan and Quran audio. A synthetic voice is not appropriate for these. | Use only real recordings the owner has permission to use. Until then the adhan is a silent on-screen notice. |
| Nasheed and narration need human voices. | Soundscapes and percussion are generated in code. Narration uses the browser's speech voice as a placeholder, with every line in a script file ready for recording. |
| Download size and tablet memory. | Streaming, compressed textures (KTX2), strict budgets checked in every phase. |
| Scope: seven regions and many systems. | One phase at a time, each ending playable. Phase 1 proves the look and feel before the world grows. |

## 8. Decisions made by the owner

- World scale, the 1910 setting, the lead-covered Dome of the Rock, Yusuf and Maryam with a 1300s ancestor,
  the music policy, the treatment of the 99 Names, and the Arabic style: all as recommended in Phase 0.
- The repository stays private for now. Builds are reviewed by running the game locally or from the
  single-file build (`npm run build:single`).
- **Adhan is specific to each place.** Every mosque in the content data names its own recording: Al-Aqsa's
  adhan in Al-Quds, and Makkah's and Madinah's when those places are added. Finding recordings that may be
  used is Phase 2 work.
- The game must teach: the land, the roads and the historical sites come first in every design choice.
- Devices: a laptop now; tablets and phones later. Touch controls exist from Phase 1a but are basic.

## 9. Phase 1a as built

- **World:** the corridor from Al-Quds to Ariha and the Dead Sea shore. Near terrain is a 5.4 x 3.5 km grid at
  3 m; far terrain is a 13.8 x 12.7 km grid at 36 m, out to the mountains east of the Dead Sea.
- **City bubbles:** `src/world/warp.ts` magnifies Al-Quds (to about 1:2.5) and Ariha so they have room.
- **Road:** the pipeline finds the road line itself. It searches the compressed terrain for a line through the
  real waypoints that avoids steep slopes and sharp turns, then smooths it, limits the slope to 13% and cuts
  the road bed into the terrain. Result: 9.5 km, with 7.6 km from Al-Quds to Ariha.
- **Wheels and ground:** the wheels roll on an exact mathematical road surface, not on the terrain grid, so
  the road is smooth whatever the terrain resolution. A small terrain collider follows the car for the chassis.
- **Differences from the plan, all to be closed in Phase 1b:** tone mapping is ACES (AgX looked washed out with
  this sky); the `postprocessing` library is not in yet; there is one shadow map around the car, not three
  cascades; there is no sound; no floating origin yet (not needed at this world size).
- **Drive time:** about 10 minutes from Al-Quds to Ariha at 45 km/h. That is longer than the 3-6 minutes the
  brief aims for between neighbouring cities. It can be shortened by raising the compression or the speeds.

## 10. Direction: the request for Forza Horizon realism

During Phase 1b the owner asked for a Forza Horizon look: real cars, realistic scenes, a simulation feel.

What stands in the way with the current approach:

- **Assets.** That look comes from scanned or hand-made models and photographic textures. Everything here is
  generated by code, which gives a clean stylised look and no more.
- **Engine.** Forza-class lighting, foliage and reflections need a native engine (Unreal Engine 5, or Unity's
  high-definition pipeline). A browser cannot do it.
- **Hardware.** The owner's laptop has integrated graphics. No engine can draw that level of realism on it.
- **Real cars.** Real makes and models need a licence from each manufacturer. Realistic unbranded cars, or
  cars old enough and generic enough to avoid trademarks, do not.
- **Tools here.** This cloud environment has no graphics card and cannot run a game-engine editor, so work in
  such an engine cannot be seen or tested from here.

What carries over to any engine: the terrain pipeline and warped map, the road routing, the content files
(places, landmarks, journal, religious texts, translations), the vehicle tuning, and the design.

Options:

1. **Stay in the browser, raise realism with real assets.** Buy or commission realistic models (a period car,
   buildings, trees) and photographic ground textures; integrate them here. Runs on the owner's laptop and on
   phones. Reaches "a good-looking web game", not Forza.
2. **Move to Unity or Unreal on a gaming PC.** The only route to the look asked for. Needs a PC with a
   dedicated graphics card for building and playing, purchased or commissioned assets, and work done on that
   machine rather than only in this cloud session. Phones and tablets become a separate, reduced version.
3. **Both, in order.** Finish the journey, story and teaching content in the browser, where it is cheap to
   change, then rebuild the visuals in an engine using the same data.
