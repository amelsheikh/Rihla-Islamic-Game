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
| E0 — Unity 6 project (owner's PC) | Done | Terrain, road, car on autopilot, child walking, Al-Quds today on Google 3D Tiles via Cesium ion, Windows build at 118 FPS. See below |
| E1 — Chapter 1 teaching systems in Unity (owner's PC) | Done | Arabic and English UI, banners, journal page with Al-Isra 17:1, history beside "Al-Quds today", saving; 102 FPS. See below |
| E2 — Walking Al-Quds, then and now (owner's PC, block-out) | Done | The walk from Bab al-Asbat to al-Masjid al-Aqsa at true scale with Jiddo, three clues and the first Time Lens reveal; 97-103 FPS. See below |
| E3 — Realism on the owner's PC | Two passes done | Stone textures, arches, battlements, trees, sky; then doors, shop shutters, windows, roof domes, warm bounce light, ambient occlusion; 64 FPS. See below |

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

## Phase E3: realism on the owner's PC, first pass (done 16 November 2026)

The owner chose to continue on their own PC instead of the cloud PC (Intel HD Graphics). The aim is the best look
this hardware allows, which also means the game will run on ordinary family laptops.

What changed in the walk (`RihlaWalkBuilder.cs`):

- **Real materials** from Poly Haven (CC0, 1K, listed in `ASSETS.md`): golden rough-faced blocks on the city
  walls, pale ashlar and white plaster on the houses and gates, pale stone slabs in the lanes, the courtyard and
  on the platform, marble on the Dome of the Rock's lower walls. All surfaces are now in metres, so stones are the
  right size everywhere.
- **Pointed-arch passages:** Bab al-Asbat and every gate or building over a street now has two piers, a pointed
  arch and solid stone above, lined up with the street, so the child walks through an arch, not under a box.
- **Battlements** along the tops of the city walls, on the outer side.
- **Trees:** the 106 trees OpenStreetMap maps one by one in this area, as olive and cypress shapes, and its grass
  and garden areas.
- **Light:** a real sky (procedural, with the low morning sun), soft shadows, filmic colour (ACES), a little glow
  and a warm morning white balance. The Time Lens's old-photograph look sits on top of this.
- **The Dome of the Rock's tiled walls** show a plain, generic tile grid. It is deliberately not a copy of the
  real patterns or the inscription, which must be made properly (`NEEDS_REVIEW.md` R5).

Checks run:

| Check | Result |
|---|---|
| Views at the gate (then and now), lane, courtyard, platform and close to the Dome, English and Arabic | `docs/screenshots/e3-*.png` |
| Windows build, the whole walk by itself (`-walk -autowalk -newgame -quitafter 660`) | All of Jiddo's lines in order; **75 FPS average** at 1280 x 720 (63-86), down from about 100 before the new look |

Known issues and what would help most next:

- **People.** The child and Jiddo are still a blue and a brown capsule; there are no other people. Proper
  characters are the biggest step left; free ones need checking for licences and for modest dress (brief section 5).
- Buildings now have doors, shops, windows and roof domes (second pass below); balconies, arches across the lanes and market stalls are still missing.
- Sound was added later (see "Sound in the walk"); the adhan still needs a real recording.
- **The ground is flat:** the real Old City slopes, and the platform's height is an estimate (H17).
- Tree species and the arch and battlement sizes are estimates (`NEEDS_REVIEW.md` H17).
- In the editor on this PC the walk shows about 20 FPS; the built game is what matters (75 FPS).

### E3, second pass: building detail and light ("make it beautiful")

- **Doors, shops and windows** on the 790 house walls that face a street or lane: arched doorways with wooden
  doors, arched shop openings with painted metal shutters, and framed windows on the upper floors, all set in
  pale stone frames (sizes and spacing are estimates, `NEEDS_REVIEW.md` H17).
- **Roofs:** a low parapet around every flat roof, and small white plastered domes on 162 houses.
- **Warmer, softer light:** sunlight bouncing off the pale stone is imitated with warm ambient light, so lanes in
  shade glow instead of going grey; screen-space ambient occlusion (half resolution) darkens corners and the foot
  of walls; paler, creamier stone on the city walls; silvery olive trees; a slightly higher morning sun.
- Fixed: the child's body is hidden in first person (the camera sat inside it); doors, windows and arches no
  longer render black (their two faces shared vertices, so their lighting cancelled out).
- Windows build, the walk by itself for 5 minutes: **64 FPS average** (56-72), from 75 before this pass.
- Screenshots: `docs/screenshots/e3b-*.png`.

### Sound in the walk (16 November 2026)

- `Soundscape.cs`, made by code like the browser prototype's sound, so no recordings are needed: a soft morning
  breeze that swells and fades; birdsong in short bursts, livelier inside al-Masjid al-Aqsa among its trees;
  footsteps on stone for the child (one every 0.7 m) and, softer, for Jiddo; two bell notes when a clue is found;
  a warm rising chord for the Time Lens reveal; and in the "then" view everything sounds slightly muffled, like a
  memory. Pages opened halve the volume.
- Checked: it plays with no errors and costs nothing measurable (65 FPS). **Nobody has listened to it yet**; the
  owner's ears decide the levels.
- The adhan is not synthesised: it needs a real recording with permission (`NEEDS_REVIEW.md` R4).

## Phase E2: Walking Al-Quds, then and now (done 16 November 2026, block-out)

Plan: `docs/ENGINE_SETUP.md` 4c, from the owner's vision in `docs/STORY.md` 8b. Built on the owner's PC as a
block-out: real outlines and working logic, plain shapes and colours, waiting for the realism pass on the cloud PC.

What works:

- **The Old City at true scale** (`E2_AlQuds_Walk`, menu *Rihla > Build E2 walk*), from OpenStreetMap data
  (`tools/build-walk.ts`, `npm run walk:build`; credit in `ASSETS.md`): 698 building outlines, the city walls
  with openings where streets pass, Bab al-Asbat with the lane through it, the compound of al-Masjid al-Aqsa and
  its own gate, the raised platform of the Dome of the Rock with stairs, the Dome of the Rock (the real octagon:
  marble below, blue tiles above, drum and dome), the Dome of the Chain and the Qibli Mosque with its dome.
- **The walk with Jiddo** (`content/walk.json`), about 600 m and 9 minutes: Jiddo walks ahead at an old man's
  pace, waits when the child falls behind, turns to look at them, and speaks at each step (subtitles, English and
  Arabic): at Bab al-Asbat, in the lane, on entering al-Masjid al-Aqsa, on the platform, at the Dome and at the
  Qibli Mosque, ending "Alhamdulillah ... the car is waiting, and the road down to Ariha."
- **Three clues** found by walking up to a glowing marker (or E): the lions of Bab al-Asbat, the tiles of the
  Dome of the Rock, the Qibli Mosque. Each opens a card whose fact is taken from `content/history.json`, so no
  new facts were written. The HUD counts them (٣ / ٣ in Arabic).
- **The Time Lens.** The walk begins "then", about 1910: an old-photograph look (faded warm colour, vignette,
  grain) and the Dome of the Rock's dark lead dome. At the top of the platform stairs Jiddo says "look again",
  and over five seconds colour returns and the dome turns to today's gold, with the banner "Today · 1959–1964
  and 1993–1994 CE". After that **T** switches between then and now at any time. The eras come from
  `content/history.json` (marked "then" and "now"), chosen in `content/walk.json`.
- **First person** with **V**: the child's own eyes, looking up with the right mouse button. The chase camera
  now keeps out of walls, so it works in narrow lanes.
- **Linked to the road:** stopped (or on foot) near Bab al-Asbat on the Jericho road, **G** walks into the Old
  City; at the start of the walk, or when it is done, **G** goes back to the car. Clues found and the walk
  finished are saved.
- For testing: `-walk` starts in the Old City and `-autowalk` makes the child follow Jiddo by itself; any build
  run with `-quitafter N` logs its frame rate in every scene.

Checks run:

| Check | Result |
|---|---|
| The whole walk, steps, clues and lines in order (editor, driven by a test script) | All 7 steps and 3 clues, ending with the walk saved as done |
| Time Lens reveal at the platform, then T both ways, English and Arabic | Working; `docs/screenshots/e2-lens-1..4-*.png`, `e2-dome-then/now-firstperson-ar.png` |
| Road to walk link | "G: walk into the Old City" shown when stopped near Bab al-Asbat |
| Windows build, `-walk -autowalk -quitafter 330` | **97.5 FPS average** over 5.5 minutes in the Old City at 1280 x 720 (91-110) |
| Windows build, the whole walk by itself in Arabic (`-walk -autowalk -newgame -arabic -quitafter 660`) | All 7 of Jiddo's lines, the Time Lens reveal and all 3 clues in order; **103 FPS average** over 11 minutes |

Bugs found and fixed: leaving a scene with a page open (for example the opening card) left the game paused in the
next scene, so every scene now starts with time running; Jiddo's route cut through a corner of the Dome of the
Rock and he could be lifted onto things overhead; and the self-walking test stuck on the platform stairs at high
frame rates (the walker's own gravity fought the test's movement; real walking was not affected).

Known issues (block-out):

- Everything is plain shapes and flat colours; no people, trees, doors, arches, shops or sound yet. The realism
  pass on this route is E3, on the cloud PC.
- Heights and the shapes of the domes are estimates, listed in `NEEDS_REVIEW.md` H17, as are Jiddo's lines.
- "Then" changes only the look and the Dome of the Rock's dome; the street itself is the same in both eras.
  Showing what else was different about 1910 needs sources (old photographs and descriptions) chosen with the owner.
- Jiddo walks through other people and objects (he has no collider) and walks straight between route points.
- The whole walk takes about 9 minutes; a faster pace when the child runs ahead may be wanted.
- No prayer, adhan or entering the buildings yet.

Next: E3, realism on the cloud PC, starting with this route (`docs/ENGINE_SETUP.md` 4c). Owner decisions waiting:
the "then" sources, how present-day streets are shown, and setting up the cloud PC.

## Phase E1: Chapter 1's teaching systems in Unity (done 16 November 2026)

Plan: `docs/ENGINE_SETUP.md` 4b. Everything the browser prototype teaches on the Jericho road now works in Unity,
in English and Arabic.

What works:

- **A real interface in both languages** (UI Toolkit, `RihlaUI.cs`), replacing E0's English-only debug box.
  Unity 6.6's *Advanced* text generator joins Arabic letters, places vowel marks and lays out right to left
  correctly (its *Standard* generator does not). The Amiri typeface is used throughout. In Arabic the whole
  layout mirrors, numbers use Eastern Arabic digits (٧١٦ م فوق سطح البحر), and "·" separators are shown as
  "—", because a middle dot looks exactly like the Arabic zero ٠. **L** or the button switches language at any
  time, even with a page open.
- **HUD:** speed, true elevation above or below sea level, the objective ("Find the first page at Khan
  al-Hatruri", with the distance) or the distance to Ariha, the nearest place in both languages, key hints, FPS.
- **Banners** on reaching each place (in both languages) and on crossing sea level.
- **The opening card** on a new game, and **the first page of the Rihla**: a golden scroll on a stone beside the
  road at Khan al-Hatruri. Stopping beside it (or walking up to it) opens the page: Zaynab's words, Jiddo's
  explanation, Surah Al-Isra 17:1 in Arabic with its reference and English meaning, and the facts. Then the
  Journal button (**J**) keeps it.
- **History of this place** (**H**) near the six places that have one in `content/history.json`, and in
  "Al-Quds today" a list of the Old City's places (the Dome of the Rock, Al-Qibli Mosque, the walls and Bab
  al-Asbat). Reading the Dome of the Rock's story through 1,300 years over the live 3D city of today is the
  first real "then and now" in the Unity game.
- **Saving:** language, pages found, the opening card seen, and where the car is (every 10 s and on quitting).
  `-newgame` starts afresh; `-arabic` starts in Arabic.
- All text comes from `content/`; new interface strings are `u.*` keys in `content/i18n/` (Arabic listed in
  `NEEDS_REVIEW.md` R6).

Checks run:

| Check | Result |
|---|---|
| Opening card, HUD, history, page, verse, banners, in both languages (editor) | Working; screenshots `docs/screenshots/e1-*.png` |
| Saving | Quit at Khan al-Hatruri, restarted there with the page and language kept and no opening card |
| Sea-level banner | Shown in Arabic on crossing 0 m (`e1-sea-level-ar.png`) |
| Windows build, 7 minutes of autopilot in Arabic (`-autopilot -newgame -arabic -quitafter 480`) | **102 FPS average** at 1280 x 720 (96-110), place banners in Arabic at Bab al-Asbat, Al-Eizariya, Khan al-Ahmar and Khan al-Hatruri |
| "Al-Quds today" with the interface (editor) | 26-29 FPS |

Known issues:

- **"Then" is told, not yet shown:** the history pages tell each place's story, but there is no picture or
  model of how it looked. Old photographs (for example the Dome of the Rock with its lead dome about 1910) or
  rebuilt models are the next step and need the owner's choice of sources.
- In the 7-minute test the game once went to "Al-Quds today" and straight back, most likely a key press
  reaching the window; keys are now ignored for the first second after a scene opens.
- Page and history text is long for a 10-year-old to read alone; Jiddo's voice (recorded or read aloud) would help.
- The car and child are still placeholders; touch and gamepad buttons for the new keys (J, H, L) are not made yet.

Next: the plan was revised for the owner's vision (`docs/STORY.md` 8b, `docs/ENGINE_SETUP.md` 4c). E2 is now
**Walking Al-Quds, then and now**: one street route from Bab al-Asbat to al-Masjid al-Aqsa at walking height,
with the first Time Lens reveal. Then E3 realism on the cloud PC, E4 ways to travel, E5 the rest of Chapter 1.

## Phase E0: Unity 6 on the owner's PC (done 16 November 2026)

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
- **Windows build** (`Builds/Windows/Rihla.exe`, menu *Rihla > Build for Windows*; not in git): 173 MB, both
  scenes. `Rihla.exe -autopilot -quitafter 70` drives itself and logs FPS to the player log.
- **Cesium for Unity 1.26.1** and a second scene, *Al-Quds today* (`E0_AlQuds_Cesium`, menu *Rihla > Build E0
  Cesium scene*): a slow circle over the Old City on Google Photorealistic 3D Tiles, with Google's credits on
  screen. C on the road opens it; arrows turn it; C or Esc goes back. The tiles come through the owner's free
  **Cesium ion** account (asset 2275207); its token is read at run time from
  `unity/Assets/StreamingAssets/cesium_ion_token.txt`, which is git-ignored (never commit a token). A Google
  Map Tiles key in `google_maps_key.txt` works instead, if one is ever set up.
- Screenshots of the city today: `docs/screenshots/e0-alquds-today-1.png` and `-2.png` (the Haram al-Sharif
  and the Dome of the Rock, the Old City, the Mount of Olives, the Dead Sea on the horizon).

Frame rate on this PC (Intel HD Graphics 4400-class): the editor's 2560 x 1440 game view ran at **10-12 FPS**
before the low settings, 17 FPS without shadows, and 112 FPS with the camera off, so the limit is the graphics
chip's drawing of pixels, not the game's logic. **The Windows build, at 1280 x 720 with the low settings,
averages 118 FPS** over 67 s of autopilot driving (123-140 FPS once loaded). The Al-Quds scene runs at
20-27 FPS in the editor with about 400 map tiles loaded.

Setting-up problems solved (written up in `docs/ENGINE_SETUP.md` 1b): the editor must be opened from Unity
Hub to get its licence; this Intel GPU crashes under DirectX 12, so the editor runs with `-force-d3d11` and
Unity's background import workers are off; Unity 6.6 makes Terrain and Vehicles optional built-in packages.

Not done yet, and known issues:

- On this PC, map tiles load more slowly than the camera turns, so grey patches show for a moment; the circle
  is slowed to 4 minutes. The owner's demo Google key was refused (HTTP 403), hence Cesium ion.
- Cesium ion's free tier is for non-commercial use ("Upgrade for commercial use" shows in the credits).
- From high above, the road shows in dashes on distant hills, where the coarse terrain detail covers it; fine
  up close. The rock layer is barely visible, so the hills read as one sand colour.
- The HUD was English only in E0; E1 replaced it with the bilingual interface.
- The car, child and buildings are placeholders (boxes and a capsule), as E0 intends.

Next (done): E1, Chapter 1's teaching systems; see above.

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
