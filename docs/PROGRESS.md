# Progress

| Phase | Status | Notes |
|---|---|---|
| 0 — Design and plan (brief section 9, step 1) | Done | Owner approved all recommendations |
| 1a — Foundation (grey-box Al-Quds to Ariha) | Done, reviewed by the owner | |
| 1b — Vertical slice: Al-Quds to Ariha | Built, awaiting owner's review | See below. The owner has since asked for a Forza Horizon level of realism with real cars; see "Direction" |
| 2 — Core systems | Not started | |
| 3 — Complete Palestine | Not started | |
| 4 — Transjordan | Not started | |
| 5 — Lebanon | Not started | |
| 6 — Egypt and Sinai | Not started | |
| 7 — Polish and release | Not started | |
| E0 — Unity 6 project (owner's PC) | Built and play-tested; Cesium waits for the API key | Terrain, road, car on autopilot, child walking, Windows build. See below |

## Phase 1a: what works

- Real terrain from Al-Quds (about 715 m) down to the Dead Sea shore (about 390 m below sea level), with the
  far landscape out to the mountains east of the Dead Sea.
- A 9.5 km road through Al-Eizariya, Khan al-Ahmar, Khan al-Hatruri, the sea-level marker and Ariha.
- One car with spring, damper and tyre physics; Family and Simulation modes; reverse; handbrake; rescue.
- Day and night: moving sun, clouds, stars, crescent moon, headlamps, shadows near the car.
- HUD in English and Arabic (right-to-left, Eastern Arabic digits): speed, true elevation, distance to
  Ariha, nearest place, clock, compass; banners on arriving at a place and on crossing sea level.
- Block-out landmarks: the walls of Al-Quds, the Dome of the Rock with its 1910 lead dome, Al-Qibli Mosque,
  villages, two khans, Maqam al-Nabi Musa, the palms of Ariha.
- Four quality presets with automatic selection, a performance test, a details overlay (F3).
- Keyboard, gamepad and basic touch controls. Chase, bonnet and free cameras. Autopilot.
- A single-file build that opens by double-click.

## Phase 1a: checks run

| Check | Result |
|---|---|
| `npm test`: autopilot drives the whole road, both modes | Passed: 9.5 km in 12.5 minutes, never more than 0.7 m from the centre line, no rollovers |
| `node tests/play.mjs`: start, drive by keyboard, menu, Arabic, performance test, on the single-file build | Passed, no console errors |
| Screenshots at 8 places and times | Taken; see `docs/screenshots/phase-1a.jpg` |
| Draw calls (High) | 46 to 123 (budget 350) |
| Triangles (High, including the shadow pass) | 430k to 670k (budget 1.5 million) |
| Download size | 7.8 MB (budget 40 MB) |
| Frame rate, owner's laptop (Intel UHD integrated graphics, 1912x914, Edge) | Low 60 fps (1% low 52), Medium 60 (58), High 60 (58), Ultra 42 (27). 60 is the screen's limit, so the spare capacity on High is unknown. |

## Phase 1a: owner's feedback

- Controls work and the game feels nice.
- Graphics, boundaries and collisions feel artificial. To address in Phase 1b: buildings and walls are plain boxes with box-shaped collisions, the ground off the road is uniformly smooth, and nothing marks the road's edge.
- The laptop has integrated graphics, and the drop on Ultra comes from the larger shadow map and longer detail distances. Phase 1b must treat Medium and High on this laptop as the target and keep shadows and post-processing cheap.

## Phase 1a: known issues

- Everything visual is a placeholder: flat ground colours, blob trees, box buildings.
- No sound yet.
- The drive from Al-Quds to Ariha takes about 10 minutes, longer than the brief's 3 to 6.
- Twilight is dim just after sunset.
- Touch controls are untested on a real phone or tablet.
- The road simply stops at the Dead Sea shore.
- Place coordinates and the road's line need checking against maps (`NEEDS_REVIEW.md` H6, H8).

## Phase 1b: what was added

- **Ground:** rock and stony-soil detail close to the camera, chosen by steepness; the road and every tree are
  marked in the ground colour so they read from far away.
- **Hill shadows across the whole view:** the land is tested against the sun and the result refreshed a little
  each frame (`src/render/sunshade.ts`). Cheap enough for integrated graphics.
- **Plants and stones:** about 5,600 olive trees, 550 cypresses, 1,300 date palms, 7,000 shrubs, 6,000 rocks,
  and kerb stones along the road, with simpler shapes at distance.
- **Buildings from a textured kit:** the walls, towers and gates of Al-Quds; the Dome of the Rock (marble and
  tile octagon, lead dome); Al-Qibli Mosque; four minarets of the Haram; Bab al-Rahma; the Holy Sepulchre,
  the Church of the Redeemer, the Citadel, the Hurva and Tiferet Yisrael synagogues, the Church of Mary
  Magdalene and the Russian tower on the Mount of Olives; Al-Eizariya; two khans; Maqam al-Nabi Musa; Ariha.
- **Road:** new surface, dusty verges that fade into the ground.
- **The Dead Sea:** shallows, deep water, a salt rim and ripples.
- **Finishing effects** on High and Ultra: glow, corner darkening, colour. Valley haze.
- **Sound, all synthesised:** engine, tyres by surface, wind, skid, birds by day, crickets by night, horn (H).
- **Story:** an opening card; the first page of the Rihla at Khan al-Hatruri, with a verse (17:1) kept in
  `content/religious_texts.json`; a Journal button; an objective line in the HUD.
- **Reset car:** a button on screen and the R key at any time; in Family mode a tipped car is put back by
  itself after about four seconds. Jiddo and the young traveller now sit in the car.

## Phase 1b: checks run

| Check | Result |
|---|---|
| `npm test` | Passed (5 tests) |
| `node tests/play.mjs` on the single-file build | Passed (13 checks): story card, driving, elevation, sound level, tipping and Reset, journal page with verse, Journal button, menu, Arabic, performance test, no console errors |
| Screenshots | `docs/screenshots/phase-1b.jpg` |
| Draw calls (High) | 58 to 182 (budget 350) |
| Triangles (High, all passes) | 310k to 830k (budget 1.5 million) |
| Download size | 7.9 MB |
| Frame rate | Not measured here. The owner's Phase 1a result was 60 fps on High; Phase 1b is heavier and must be re-measured. |

## Phase 1b: known issues

- **The sound has not been heard by anyone yet.** It was built and level-checked by measurement only.
- The look is stylised, not photorealistic: shapes are simple and textures are generated.
- Midday in the wilderness looks hazy and pale.
- Shadows of trees and buildings only appear near the car.
- The inscription band of the Dome of the Rock is left plain on purpose (`NEEDS_REVIEW.md` R5).
- Arabic text was written by the developer and needs a native reader's review (`NEEDS_REVIEW.md` R6).
- Touch controls and the gamepad are still untested on real devices.

## Direction: a realistic browser game (current)

After weighing a game engine, the owner chose to stay in the browser for now and push realism as far as it
goes there. Everything is built and checked in the cloud session; nothing needs the owner's own computer.
The earlier engine plan is kept in `DESIGN.md` sections 10 and 11 for later.

The owner's purpose, in their words: to teach their children, explore their historical home with them, and
connect them to their roots and to Islamic history. Teaching comes first; graphics serve it.

Decisions:

- **Setting:** the present day, with modern cars and modern roads, and no borders, walls or checkpoints drawn.
- **History:** every important site can show its story through time, and where possible how it once looked.
- **Walking:** the player should be able to leave the car and walk. Not built yet.
- **Car:** realistic and unbranded, in the style of a modern electric crossover.

## Realism pass R1: what was built

- **Photographic materials** from Poly Haven (free, CC0): rock, stony soil and cracked valley mud on the
  ground, asphalt on the road, weathered limestone blocks on the walls of Al-Quds.
- **A modern road:** two asphalt lanes, painted edge and centre lines, gravel verges, and steel guard rails
  wherever the ground drops away, which the car cannot pass through.
- **A modern car:** a sleek crossover shaped by code, with metallic paint, a glass roof, alloy wheels, brake
  lights and headlamps; handling retuned (125 kW, about 137 km/h, six gears) and a quieter engine sound.
- **Today's Al-Quds:** the Dome of the Rock has its golden dome.
- **History of this place:** near the Dome of the Rock a button opens its timeline from 691 CE to today, and
  "See it as it was" shows the dark lead dome of about 1910. Content is in `content/history.json`.
- **Story:** the opening now fits the present day. The journal page is unchanged.
- **Fix:** ground detail now refreshes at once after Reset car or Back to Al-Quds.

## Realism pass R1: checks run

| Check | Result |
|---|---|
| `npm test` | Passed (5 tests) with the new car |
| `node tests/play.mjs` on the single-file build | Passed (15 checks), including the history timeline and the "See it as it was" switch |
| Screenshots | `docs/screenshots/realism-r1.jpg` |
| Draw calls (High) | 50 to 155 (budget 350) |
| Triangles (High, all passes) | 410k to 830k (budget 1.5 million) |
| Download size | 11 MB (12 MB as a single file) |
| Frame rate | Not measured here; the owner runs the performance test |

## Realism pass R1: known issues

- The car is built by code. It has the outline of a modern crossover but not the detail of a real model.
- Trees are still simple shapes; they are now the least realistic thing on screen.
- Villages, khans and Ariha still look historical; modern towns are not built.
- The road follows the line found by the computer, not today's real highway.
- The Dead Sea is at the level in the elevation data (about 411 m below sea level); today it is lower.
- The sound has still not been heard by anyone.
- No walking yet.

## Direction change: to Unity on a cloud graphics PC (after R2)

The owner judged that the browser has reached its limit and asked for a dedicated graphics engine. The owner has
no powerful computer, so the plan is a rented cloud Windows PC with a graphics card (Shadow PC to start), running
Unity 6 and Claude Code. Full steps and costs: `docs/ENGINE_SETUP.md`. The first engine phase, E0, is described
there. The browser build stays as the prototype for story and teaching content.

Story decisions recorded in `docs/STORY.md` section 8: the child visits from abroad, the child prays, the rest of
Palestine comes after Chapter 1, "then and now" is central, and the world should have cities, shops, people,
walking and flying.

Waiting for the owner: to choose and set up the cloud PC, and a Google Maps API key for flying over real cities.

## Phase E0: Unity 6 on the owner's PC (first build done 16 November 2026; Cesium still to come)

Working on the owner's own PC first (`docs/ENGINE_SETUP.md` section 1b): Intel HD Graphics, 16 GB memory.
Unity **6000.6.9f1** (Unity 6.6), URP 17.6, new Input System, Unity Pipeline package (so Claude drives the
editor through the Unity command-line tool that comes with Unity Hub).

Done:

- Repository cloned; Git 2.47 was already installed; Unity Hub 3.22.6 installed with winget.
- **Export for Unity** (`npm run unity:export`, `tools/export-unity.ts`): reads the browser game's generated
  terrain (`src/generated/`) and writes `unity/Assets/Rihla/Generated/`: twelve 1.5 km terrain tiles at 3 m
  (513 x 513, 16-bit), one 13.8 km far terrain with a hole under the tiles, and `scene.json` with the road
  (3,203 points, 9.6 km), the 13 places, the Old City wall line and the Dead Sea level, all turned to Unity's
  axes (+z north). The road lies within 0.44 m of the exported ground (the road bed is carved 0.3 m).
- **Content** is copied unchanged into `unity/Assets/Rihla/Content/`; `content/` stays the one place to edit.
- **The URP project** in `unity/`, made from Unity's URP template. Menu *Rihla > Build E0 scene* builds
  `Assets/Scenes/E0_JerichoRoad.unity`: 13 terrains (soil and rock blended by slope), the road mesh with a
  dashed centre line and collider, the Dead Sea, block-out Old City walls, gold place markers, the car, the
  child, sun, sky and haze. Generated assets go to `Assets/Rihla/Built/` (not in git; rebuild from the menu).
- **Play-tested in the editor:** the placeholder car drives itself down the road on autopilot (P, or
  `-autopilot` on the command line) at about 60 km/h, staying on the road; WASD/arrows or a gamepad drive it,
  R puts it back on the road. Stopped, E lets the child out: the child walks on the road and hillside, stays on
  the ground, and the chase camera follows; E near the car gets back in. The HUD shows speed, true elevation
  (677 m near Al-Eizariya, as the browser game), distance to Ariha, the nearest place and FPS.
- Lowest quality: the *Mobile* quality level for Windows, render scale 0.6, shadow distance 50 m, hard shadows,
  no MSAA or HDR, coarser terrain detail; the build opens in a 1280 x 720 window.
- Screenshots: `docs/screenshots/e0-overview.png` (Al-Quds east to the Dead Sea), `e0-road-00/25/50/75/98.png`
  along the road, and `e0-play-on-foot.png` (the game view in play, with the HUD).
- Windows build (`Builds/Windows/Rihla.exe`, menu *Rihla > Build for Windows*): being made; results to follow.

Frame rate on this PC (Intel HD Graphics 4400-class): the editor's 2560 x 1440 game view ran at **10-12 FPS**
before the low settings, 17 FPS without shadows, and 112 FPS with the camera off, so the limit is the graphics
chip's drawing of pixels, not the game's logic.

Setting-up problems solved (written up in `docs/ENGINE_SETUP.md` 1b): the editor must be opened from Unity
Hub to get its licence; this Intel GPU crashes under DirectX 12, so the editor runs with `-force-d3d11` and
Unity's background import workers are off; Unity 6.6 makes Terrain and Vehicles optional built-in packages.

Not done yet, and known issues:

- **Cesium flying over Al-Quds (step 6)** waits for the owner's Google Maps API key.
- From high above, the road shows in dashes on distant hills, where the coarse terrain detail covers it; fine
  up close. The rock layer is barely visible, so the hills read as one sand colour.
- The HUD is English only, since Unity's simple debug UI does not join Arabic letters; the real UI (E1) will.
- The car, child and buildings are placeholders (boxes and a capsule), as E0 intends.

Next: Cesium over Al-Quds once the key is in; then, on the cloud PC, the realistic look (E1).

## Pass R2: walking, history, driving and sound

Owner's feedback on R1: driving felt a bit off, the car sounded wrong, and the laptop ran it slowly
(Intel HD Graphics: Low 40 fps, Medium 21, High 13, Ultra 12). The owner asked for walking and more history
views, and now wants to focus on the story before any move to a more advanced engine.

What was built:

- **Walking:** Get out and walk (button or E) when the car has stopped; Get back in when beside it. Arrow keys or
  W A S D to walk and turn, Shift to run, drag to look around; footsteps. People on foot can pass through the
  gate of Bab al-Asbat and step up onto the Haram platform; the car cannot. Old City houses are now solid with
  lanes between them. The journal page can be picked up on foot.
- **History views** for five more places: Al-Qibli Mosque (with the hadith of the three mosques), the walls and
  gates of Al-Quds, Maqam al-Nabi Musa, Ariha and the Dead Sea. The History button opens the nearest one.
- **Driving:** quicker steering, more steering at speed, quicker return to centre, a lighter stability assist.
- **Sound:** the petrol engine is replaced by the quiet whine of an electric car, with softer tyre and wind
  noise, footsteps and a modern horn.
- **Speed on modest laptops:** Intel graphics now start on Low; Low skips the close-up photographic ground
  detail, draws less far, and renders at a lower resolution.
- **Story design:** `docs/STORY.md`, a draft for the owner's review.

Checks: `npm test` passed (5). `node tests/play.mjs` passed (18 checks, now including getting out, walking and
getting back in). Walking screenshots: `docs/screenshots/walking-r2.jpg`. Frame rate on the owner's laptop
not yet re-measured.

Known issues: walking through the gate passes through the tower block rather than an opening; the Dome of the
Rock and the Qibli Mosque cannot be entered; the walker's figure is simple; the sound has still not been heard.

## Next (proposed)

1. The owner reviews `docs/STORY.md` and answers its questions.
2. Build Chapter 1 of the story as described there.

Earlier ideas still open: better trees, today's real road line, a purchased car model.

## Previously proposed (R1)


1. Walking: get out of the car at a site and explore on foot.
2. History views for more sites, starting with Al-Qibli Mosque, the walls and gates, Ariha and Maqam al-Nabi Musa.
3. Better trees and roadside detail; today's real road line from OpenStreetMap.
4. A purchased or licensed car model, if the owner wants a truly realistic car.

## Still open with the owner

- Who reviews religious texts, history dates and the Arabic.
- Adhan recordings for each place.
- Whether to buy a realistic car model.
