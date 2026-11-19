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
| E3 — Realism on the owner's PC | Three passes done | Stone textures, arches, battlements, trees, sky; then doors, shop shutters, windows, roof domes, warm bounce light, ambient occlusion; then sound, and people in modest dress (Jiddo, Yusuf or Maryam, 55 townspeople with children and elders, in 1910 dress in the "then" view); 62-70 FPS. See below |
| R4 — "As if visiting" on the laptop | Done 19 November 2026 | A photographed sky with clouds, weathered pale limestone, lane life (awnings, plants, shutters, arches across lanes; lamps, cables and water tanks only today), leafy trees, windows in the Dome of the Rock; 63-66 FPS. A prototype on the owner's Desktop. See below. The plan ahead is in `docs/ROADMAP.md` |
| C1a (first part) — The opening at home, the storybook, guidance | Done 19 November 2026 | The game now starts in Jiddo's house: Jiddo reads Yusuf and Maryam a story from his old book, a page and a map for each place (Makkah to Al-Quds), then the walk at dawn; a goal box and golden arrows guide every step; the gate into al-Aqsa and the stairs to the Dome were blocked and are open. Opening 71-79 FPS, walk 66 FPS. See below |
| C1a (second part) — Jiddo's chest, Page 1, clue boxes, truer Qibli Mosque and Dome, breeze | Done 19 November 2026 | After the walk the child goes home: Jiddo opens the family chest, the child reads Zaynab's first page and solves her riddle on a map (Ariha), then the drive. Clues wait in floating boxes embroidered with tatreez; the Qibli Mosque has its seven-arched porch, raised nave and lead dome, the Dome of the Rock its drum and pointed dome; a breeze, rustling and blowing leaves, crickets at home. Same speed as the last prototype on the laptop today. See below |
| C1b (first part) — Guidance on the drive | Done 19 November 2026 | Prototype `Builds/Rihla-prototype-2026-11-19d.zip` on the Desktop. The road down to Ariha now has the walk's goal box and golden arrows: to the page at Khan al-Hatruri, to where the road goes below the sea, then to Ariha; back to the road, back to the car, Jiddo driving. 108 FPS with Jiddo driving. See below |
| C1b (second part) — Jiddo's remarks, Khan al-Ahmar, the shepherd, a softer guide | Done 19 November 2026 | Prototype `Builds/Rihla-prototype-2026-11-19e.zip` on the Desktop. The golden arrows on the road are replaced by one soft golden arrow in the air ahead with a pool of light under it; Jiddo talks about the land at nine places; a stop at the ruin of Khan al-Ahmar; a shepherd crosses with his flock and the child waits. 100 FPS with Jiddo driving. See below |

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

## C1b, second part: Jiddo's remarks, Khan al-Ahmar, the shepherd, and a softer guide (done 19 November 2026)

The owner asked for Jiddo's remarks, Khan al-Ahmar and the shepherd, and found the golden arrows on the road "not
looking good": "more subtle, like maybe a light on the road or one light arrow in the air".

- **One soft arrow** (`DriveGuide.cs`): the trail of arrows is gone. One slim golden arrow, see-through and glowing,
  floats about 2.6 m over the road 16 to 38 m ahead of the car (further when faster), leaning forward and turning
  with the road, with a soft pool of light on the road under it. It dims while the child drives the right way and
  fades almost away after a few seconds of good driving; it brightens when the car stops, turns back or leaves the
  road (then it hangs over the road, pointing back to it). Near a goal it glides over the goal and points down. It
  hides while Jiddo drives, while he talks at the khan and while the flock crosses. The goal box says "follow the
  golden light".
- **Jiddo's remarks** (`DriveStory.cs`, `content/drive.json`): nine short lines at their places on the road, one at a
  time and never over another line: the way down to Ariha; the hills that catch the winter rain from the sea, so
  olives grow; the dry land in their shadow, the wilderness; shepherds there for thousands of years; your ears as
  the road falls more than a thousand metres; Wadi al-Qilt and its springs; the warm air below sea level; Maqam
  al-Nabi Musa, peace be upon him, in the hills; the green oasis of Ariha ahead.
- **Khan al-Ahmar**: the ruin of a walled courtyard inn beside the road, partly fallen, with its gateway towards the
  road and a row of vaulted rooms (an estimate, H22). It is the first goal: "Drive down to Khan al-Ahmar, the old
  travellers' inn", then "Slow down and stop by the old khan". Stopped beside it (or on foot at it), a banner ("The
  Red Inn, where travellers rested for centuries") and four lines from Jiddo: what a khan is; that in the Mamluks'
  time, about seven hundred years ago, it became an inn for travellers and pilgrims, in Zaynab's time; the name and
  the reddish rocks; Bedouin families near it today with their flocks. A child who drives past is told it can wait
  for another time. It is remembered in the save (`SaveData.stops`).
- **The shepherd and his flock** (`Flock.cs`): a shepherd in a dark thobe, brown cloak and kufiya with his staff, and
  sixteen animals (white sheep with brown faces and broad tails, and black and brown goats), grazing beside the road
  after Khan al-Hatruri at a straight stretch the builder picks so they are seen from afar. When the car comes
  near, they cross the road: the shepherd first, the flock trotting after him in a loose bunch, bleating (a voice
  made by code). Jiddo: "Look, a shepherd with his flock! Slow down and stop. We let them cross." The goal box
  says to wait. A child who rushes is told "Gently!", and the car is held just before the flock. When they are
  across: "As-salamu alaykum!", the shepherd's "Wa alaykum as-salam! Thank you for waiting. Go safely.", and Jiddo's
  "Well done. You waited kindly" (or, after rushing, "The flock was on the road first..."). Adab shown, not taught.
- **Jiddo driving** stops at the khan to tell about it and slows to a stop for the flock, then drives on.
- The goal "Drive on to Ariha" now ends at Ariha itself (7.6 km), not at the end of the road by the Dead Sea.

| Check | Result |
|---|---|
| Windows build, Jiddo driving from Bab al-Asbat for 10 minutes (`-newgame -drive -autopilot -quitafter 600`) | **100.4 FPS** average (lowest 78.8), no errors. In order: eight of Jiddo's nine remarks, the stop at Khan al-Ahmar with its four lines, the flock crossing with the greeting, thanks and "Well done", sea level; the run ended before the last remark (Ariha ahead) |
| In the editor, the car placed before the khan, at it, before the flock, and below Khan al-Hatruri in Arabic for Maryam | Goals, Jiddo's lines and the shepherd's in English and Arabic (feminine forms correct); the arrow ahead, over the goal, and hidden while the flock crosses |
| Pictures | `docs/screenshots/c1b2-khan.png` (stopped at Khan al-Ahmar), `c1b2-flock.png` (the flock crossing), `c1b2-flock-thanks.png` (the shepherd's thanks), `c1b2-arrow-ar.png` (the soft arrow, in Arabic, for Maryam) |

Not yet tried by hand at the keys from start to end. Still to do in C1b: Ariha itself (on foot to the spring and
Tell al-Sultan, Umm Khalil and Page 2).

## C1b, first part: guidance on the drive (done 19 November 2026)

The owner chose "guidance for the drive" next, so the road from Al-Quds down to Ariha is guided like the walk
(`DriveGuide.cs`, texts in `content/i18n`, the "d." strings, with feminine Arabic for Maryam):

- **The goal box** at the top always says what to do now, with the distance in metres or kilometres (for the first
  half-minute, the keys instead). In order: drive down to Khan al-Hatruri; near it, slow down and stop beside the
  golden scroll (on foot: walk to it); then drive on down and watch for the place where the road goes below the
  level of the sea; then drive on to Ariha, the city of palms; and at the end, "You have reached Ariha! Its story
  comes next."
- **Golden arrows** along the road ahead, up to 130 m, towards the goal, bigger than the walk's and standing up at
  an angle so they read from behind the car (flat on the road they looked like thin lines). A big golden arrow
  floats over the goal when it is within 600 m: the scroll, the sea-level point, Ariha.
- **Help when lost:** off the road by more than 22 m, the box says to drive back to the arrows or press R to return
  to the road; out of the car away from the page, to walk back and press E; with Jiddo driving, "look at the land,
  press P to drive yourself".
- The golden scroll at the khan and its beam are now unlit, so they glow.

| Check | Result |
|---|---|
| Windows build, Jiddo driving from Bab al-Asbat (`-newgame -drive -autopilot -quitafter 200`) | **107.8 FPS** average, no errors |
| In the editor, the car placed along the road (600 m and 60 m before the khan, the page found, 400 m before and 200 m after sea level, 60 m from the end) | The goal changes at each place as listed above |
| Pictures | `docs/screenshots/c1b-drive-guide.png` (below the khan, towards sea level), `c1b-drive-khan-ar.png` (in Arabic, for Maryam, towards the khan) |

Still to do in C1b: Jiddo's remarks on the land along the way, Khan al-Ahmar, the shepherd and his flock, and
Ariha itself. Where the road goes below sea level is taken from the game's terrain heights, which are compressed
(the same as the sea-level banner).

## C1a, second part: the chest, Page 1, clue boxes, the two buildings, and sound (done 19 November 2026)

The owner chose "Jiddo's chest and Page 1" next, and after trying the game asked that the Qibli Mosque and the
Dome of the Rock look as they really are, that clues be kept in "a hovering small treasure box with a Palestinian
symbol on it", and that sounds and effects begin: breeze, leaves moving, calm.

What changed:

- **Back home: Jiddo's chest** (`StoryOpening.RunChest`, `ZaynabPage.cs`, text in `content/storybook.json`
  "chest"). When the walk ends Jiddo says "let us go home", the screen fades, and the evening room opens on the
  family chest. Jiddo tells the children it holds the pages of Zaynab al-Maqdisiyya; the lid creaks open, a warm
  light and an old sealed page rise out of it. The page opens like Jiddo's book: on the left Zaynab's letter (why
  she set out, and that each page leads to the next), on the right her riddle with a red seal. Then the riddle on a
  map of the land around Al-Quds (coast, the Jordan, the Dead Sea): three places to choose, Yafa, Ariha and
  al-Khalil, by button or on the map. A wrong choice gives Jiddo's kind hint ("Yafa is on the shore of the sea, not
  below it"); Ariha draws the road on the map, Jiddo says they will drive down in the morning, and the drive
  begins. The page is saved as found. `-chest` starts this scene directly, for testing.
- **Clue boxes.** Each clue now waits in a small wooden box floating over a golden ring, embroidered on every side
  and on the lid with red cross-stitch on cream linen (an invented eight-pointed star in the manner of tatreez),
  with brass corners and turning sparkles. It bobs and turns; when the child walks into the ring the lid opens, the
  light comes out, and the box shrinks away (`ClueBox.cs`). The same embroidery is on the front of Jiddo's chest.
  The goals now say "walk to the glowing box".
- **The Qibli Mosque** is rebuilt in its real shape from its OSM outline: the long hall running south, side aisles
  under flat lead roofs, a raised central nave with high windows and a long pitched lead roof, a lead dome on a drum
  over the bay before the mihrab at the south end, and across the north front the porch of seven pointed arches
  with battlements. Before, it was one plain block with a ball on it.
- **The Dome of the Rock** now has a sloping lead roof up to a tall tiled drum with sixteen arched windows, and a
  slightly pointed dome about 20 m across with a finial and crescent; before, a short drum and a stretched ball.
  Its lower walls are lighter, like marble. All proportions are estimates (NEEDS_REVIEW.md H20).
- **Sound and effects.** The breeze now rises and falls in gusts about every half minute. Near trees the leaves
  rustle when it rises, and a few small leaves tumble past in the air (`LeafDrift.cs`, one particle system that
  follows the camera). At home in the evening: crickets outside and a softer breeze, no birds; the chest's lid
  creaks. All made by code, no recordings; no music, as the brief asks (nature sounds by default).
- **Glow.** Glowing materials in the walk (the clue rings and sparkles, today's street lamps) are now unlit and
  bright, so they glow; the old glow setting was lost when materials were saved.

Checks run:

| Check | Result |
|---|---|
| Windows build, the chest scene by itself (`-newgame -chest -autowalk`) | Letter, riddle, a wrong choice with its hint, the right choice, then the drive loads; 48.8-55.0 FPS; no errors |
| Windows build, the walk to its end, home to the chest, then the drive (`-newgame -walk -autowalk -quitafter 500`) | The walk with all three clues (the boxes open), Jiddo's last line, home, the chest, the riddle with a wrong then the right choice, then the drive loads (the Bab al-Asbat banner); walk **46.9 FPS** average over the first 500 s; no errors in English. In Arabic it found one error, fixed: the metres line of the goal box failed every frame, because the placeholder's 0 was turned into an Arabic digit (this was also in the last prototype). After the fix, 45 s of the walk in Arabic: no errors, 47.2 FPS |
| Speed, the new build against the last prototype, the same 40 s of the opening, one after the other | 51.0 FPS and 49.7 FPS: the same. The laptop ran slower today than when the last prototype was measured (it gave 78.8 then, about 50 now), so today's numbers are lower for both |
| Pictures | `docs/screenshots/c1a2-*.png`: the Qibli Mosque, the Dome of the Rock, a clue box, the chest, Zaynab's page, the riddle (taken before Jiddo's subtitle was hidden on the page) |
| The prototype | `Builds/Rihla-prototype-2026-11-19c.zip` (75.5 MB), unpacked to the owner's Desktop as **Rihla prototype**, replacing the earlier copy. It contains the Cesium token, so it stays in the family. The game pauses when its window is not in front (seen in testing) |

Known issues and next steps:

- The buildings are closer to the real ones, but still simple shapes; their proportions wait on photographs
  (the owner offered to send some) and a check (H20). The porch arches are drawn on the wall, not open.
- The Dome of the Chain is still a plain ring with a ball; it is an open arcade of columns in reality.
- Trees do not sway yet (that needs a wind shader); only the leaves blowing past move.
- The adhan and the prayer moment still wait on the owner's and a scholar's decision.
- The drive (C1b) still has no goal box or arrows.
- Zaynab's letter, the hints, the tatreez and the new Arabic are on the review list (H20, H21, L3).
- Jiddo's riddle line and hints are shown on the page, not as subtitles, so they do not cover its button.

## C1a, first part: the opening at home, the storybook, and guidance (done 19 November 2026)

The owner tried the R4 prototype and found it confusing for young children: there was no opening, the player
did not know where to go, and the lane to al-Aqsa seemed to end in a wall. They asked for an opening in a cozy
living room where a grandparent tells the grandchildren a story from a book about the history of the Muslims and
of the blessed land, then the pages of the book showing the places one by one, and a game guided step by step,
with instructions and arrows.

What changed:

- **The way into al-Aqsa was blocked, and is open.** The gate from the Bab al-Asbat lane into the courtyard of
  al-Masjid al-Aqsa was built as solid stone: the code that cuts an arch through a gate took its line from a
  covered passage on another street. A passage now only counts if it runs through that building. The building
  over the north stairs to the Dome of the Rock (the arcade at their top) also blocked the stairs; it is now built
  with a raised arch over them. A test sweeps the whole of Jiddo's route for walls in the way: none are left.
- **The opening at home** (new scene `C1_Home`, built by `RihlaHomeBuilder.cs`, run by `StoryOpening.cs`): an
  evening in Jiddo's house in the Old City. A vaulted room with patterned cement tiles, floor seating in red woven
  cloth, a rug, a hanging brass lantern, a low table with tea and dates, a bookshelf, the family chest, a picture
  of the Dome of the Rock, and a window onto the evening sky with the Dome far off. Jiddo sits with his old book,
  Teta beside him, Yusuf and Maryam on cushions before them. The camera moves in while Jiddo speaks, then down to
  the book as he opens it.
- **The storybook** (`StoryBook.cs`, text in `content/storybook.json`): an open book on the screen. On the left
  page a hand-drawn map of the lands (coasts, rivers and lakes from Natural Earth, no borders), with the page's
  place marked in red; on the right its name in Arabic and English, two or three lines, and when the child will
  visit it. Ten pages: Makkah, al-Madinah, Al-Quds (with the way of al-Isra' drawn from Makkah), Dimashq,
  Baghdad, al-Qahira, Sayda, al-Khalil, Ariha, and Al-Quds our home. Space, Enter, the arrow keys (right to left in
  Arabic) or the button turn the page, with the sound of a page. Then Jiddo says they will walk to al-Masjid
  al-Aqsa at dawn and asks who will walk with him first: the player chooses Yusuf or Maryam. Esc skips the story.
- **Guidance in the walk** (`WalkGuide.cs`, `WalkDirector.Guidance`): a box at the top of the screen always says
  what to do now, in words for children ("Follow Jiddo. Walk along the golden arrows.", "Find the carved lions
  beside the gate. Walk into the golden light.", "Stop and listen to Jiddo."), with the distance or, for the first
  half-minute, the keys. Golden arrows slide along the ground on Jiddo's road to the goal, and a golden arrow
  bobs over Jiddo or the clue. A child who wanders off is told to follow the arrows back. The texts are in
  `content/walk.json` ("goals" and each clue's "goal"), with feminine Arabic for Maryam.
- **Glow fixed.** Glowing materials lost their glow setting when saved; the window, the lantern and the guide
  arrows now use an unlit material bright enough to glow. (The walk's clue markers and today's lamps still use
  the old setting and do not glow; to fix the same way.)
- The game starts with the opening. Start files in the prototype: *1 - Start the story*, *2 - Walk in Al-Quds*,
  *3 - Drive from Al-Quds* (`-walk` and `-drive` start those scenes directly).

Checks run:

| Check | Result |
|---|---|
| The route of the walk, swept with the child's size for walls in the way | Before: blocked at the gate into al-Aqsa and at the north stairs. After: clear |
| Windows build, the whole start (`-newgame -autowalk -quitafter 160`): the opening, the book, then the walk | Opening **71-77 FPS**, walk **66.1 FPS** average, no errors; the test walk passes through the gate into al-Aqsa |
| The walk by itself, 200 s (`-walk -autowalk`) | **66.0 FPS**, no errors |
| The new prototype copy on the Desktop, 40 s | 78.8 FPS in the opening |
| Pictures | `docs/screenshots/c1a-*.png`: the room, the book (Arabic and English), the guide, the open gate and stairs |

The prototype: `Builds/Rihla-prototype-2026-11-19b.zip`, unpacked to the owner's Desktop as **Rihla prototype**
(the earlier copy was replaced). It contains the Cesium token, so it stays in the family.

Known issues and next steps:

- The figures are still simple; seated figures are posed, with rounded legs added under the robes.
- In the 1910 view the colouring turns the golden arrows cream; they still stand out.
- The test walk moves the child by itself, so it shows the route works but not how a child at the keys finds it.
- The rest of C1a: after the walk, back home, Jiddo opens the chest and Page 1 (Zaynab's first page and riddle);
  the adhan and the prayer moment wait on the owner's and a scholar's decision.
- The drive (C1b) has no goal box or arrows yet; it should get the same guidance.
- Every caption of the book, Jiddo's new lines and the room are listed in `NEEDS_REVIEW.md` (H19, L2).

## R4: "as if visiting", realism on the laptop (done 19 November 2026)

The owner asked to take the graphics further, as if visiting, on this laptop for now (Intel HD Graphics), to
revisit the plan, and for a prototype to test. The plan is now in `docs/ROADMAP.md`.

What changed in the walk:

- **A real sky.** A photographed morning sky with clouds (Poly Haven, CC0), turned so that its sun is where the
  scene's sun is; the sun now stands 38 degrees up, as in the photograph.
- **Light in the shade.** Unity was not using the sky colours set for the shade (its stored ambient light only
  changes when lighting is baked), so a small script (`SkyFill.cs`) sets it when the scene starts: a cooler,
  brighter fill from the sky, warm from the ground. Lanes in shade are now pale and cool, not brown.
- **Weathered stone** (`Shaders/RihlaWeatheredStone.shader`, URP's Lit shader with a changed colour step): walls
  are paler and creamier, like Jerusalem limestone; large and small patches of lighter and darker stone break up
  the texture's repeat; hue changes from wall to wall; faint rain streaks run down the walls; grime and damp darken
  the foot of every wall; paving has worn patches. At most three texture reads per pixel, for integrated graphics.
- **Life in the lanes** (`RihlaWalkLife.cs`): 203 cloth awnings over shops, 224 potted plants by doors, wooden
  shutters beside 1,400 windows, and 17 arches across narrow lanes where both sides are built, in both eras.
  Only in today's view: 257 street lamps on iron brackets, 13 overhead cables, and black water tanks on 313
  roofs. All of it is set dressing placed by code, not a record of particular houses (`NEEDS_REVIEW.md` H17).
- **Trees** are now crowns of leaf cards (an alpha-tested leaf texture made by code) around a darker core,
  instead of smooth balls.
- **The Dome of the Rock** has tall arched windows in its tiled walls, and its generic tiles vary a little in
  shade; the real tile patterns are still not copied (`NEEDS_REVIEW.md` R5).
- Cost savings to stay above 60 FPS: small details flat on walls cast no shadows, leaf cards are drawn once
  (two-sided material), and the stone shader was cut from five texture reads to three.

Checks run:

| Check | Result |
|---|---|
| Windows build, the walk by itself (`-walk -autowalk -newgame -quitafter 95`, the "then" view throughout) | **62.9 and 66.1 FPS** average, no errors (before the savings: 57-59.5) |
| The prototype copy on the Desktop, 40 s of the walk | 61.0 FPS, no errors |
| Views in both eras: lane, the Dome of the Rock, roofs from above | `docs/screenshots/r4-*.png` (interface hidden for the pictures) |

The prototype for the owner: `Builds/Rihla-prototype-2026-11-19.zip` (75 MB, made by
`tools/prototype/package.py`), unpacked to the owner's Desktop as **Rihla prototype**, with *1 - Walk in
Al-Quds* and *2 - Drive from Al-Quds* start files and a *HOW TO PLAY* note. It contains the Cesium token, so
it stays in the family.

Known issues and what would help most next:

- Buildings are still plain boxes of one height each, with flat roofs; the ground is flat (the Old City slopes).
- The lamps' glass is a plain glowing box; the awnings are flat sheets.
- The Dome of the Rock's tiles and the Qibli Mosque are still generic block-outs (`NEEDS_REVIEW.md` R5).
- The FPS test runs in the "then" view; today's view adds lamps, cables and tanks (no shadows, small cost),
  not measured separately.
- After editing a builder script, wait for the editor's code reload before rebuilding the scene, or the old
  code runs (it happened once in this pass).

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

### People in the walk (16 November 2026, the owner chose "simple figures now")

- `FigureBody.cs` builds each person from smooth shapes when the walk starts, with no downloaded models: a robe or
  a shirt and trousers, sleeves to the wrist, a head with no facial features, and a kufiya, prayer cap, headscarf
  or hair. Arms swing and feet step in time with walking; standing people breathe gently.
- **Jiddo Ibrahim:** a white thobe, a black-and-white checked kufiya falling behind his shoulders, a black agal,
  and a grey beard.
- **The child:** Yusuf (blue shirt, dark trousers, white trainers) or Maryam (a long plum dress and a cream
  headscarf). The choice is saved (`SaveData.hero`) and made on a card (see "Choosing Yusuf or Maryam" below).
- **46 townspeople** (`Townsfolk.cs`): 22 walk the lanes near the route, keeping to one side; 14 cross the
  courtyard of al-Masjid al-Aqsa, keeping off the platform and away from the buildings; 5 pairs stand talking.
  They pause at the end of their walk and stop to let the child pass. Men wear a thobe with a kufiya or a white
  cap, or a shirt and trousers; women wear long dresses and headscarves; some older women wear a black thob with
  red tatreez on the chest and hem and a white veil. Who wears what is an estimate (`NEEDS_REVIEW.md` H18).
- They are hidden beyond 85 m (the haze hides them there anyway), so the walk is no slower: the Windows build
  averages **68 FPS** over the first 95 s of the walk (63-70), with no errors.
- Not yet (the 1910 dress, children and elders are now done, see below): or elders walking slowly with a stick yet; no one prays, because how prayer is shown is for the owner
  and a scholar to decide (brief section 5).
- Screenshots: `docs/screenshots/e3c-*.png`.

### Choosing Yusuf or Maryam (16 November 2026)

- A card asks "Who walks with Jiddo Ibrahim?" and shows the two children side by side, each as a small figure in
  their own colours with their name; the chosen one has a gold border. It is in English and Arabic (right to left).
- When it appears: on the Jericho road just before the opening card, the first time the game is played (so the
  opening card already speaks to the chosen child); and at the start of the walk if no one has chosen yet. **C** on the walk opens it again to change, and the child in the
  scene is rebuilt at once as the other child, with a banner. Closing the card without picking keeps Yusuf.
- Saved in `SaveData.hero` and `SaveData.heroChosen`; `-maryam` on the command line still picks Maryam.
- Code: `TeachingPages.ChooseHero`, `RihlaUI.PersonCard`, `FigureBody.Rebuild`; strings `hero.*` and `w.hero` in
  `content/i18n`.
- Checked in the Editor: the card in both languages, picking Maryam by the card (saved, child rebuilt as Maryam,
  card closed), and on the road the card followed by the opening card.
- Windows build: **60.7 FPS** average over 95 s of the walk on two runs (57-68), no errors. That is lower than
  the 68 FPS of the earlier run, by about the same share in every 10 s stretch; the card does no work while
  walking, so this is most likely the laptop's integrated graphics running warmer, but it was not proven.
- **Arabic speaks to Maryam as a girl** (the owner's choice): when Maryam is chosen, Arabic text uses feminine forms,
  "وجدتِ دليلًا", "اضغطي E لتنظري عن قرب", and Jiddo says "انظري… ماذا ترين؟". UI strings take a feminine version
  from the key with ".f" added in `content/i18n/ar.json` (33 strings); bilingual fields take "ar_f" (Jiddo's five
  walk lines that speak to the child in `content/walk.json`, his words on the first page in `content/journal.json`).
  `Lang.T` and `Lang.Pick` choose them. English is unchanged. The choosing card itself avoids gender, as nobody has
  chosen yet. The feminine forms need a native speaker's check (`NEEDS_REVIEW.md` L1).
- Fixed: `-newgame` reset the save after `-maryam` had been read, so a build started with both played Maryam in
  Yusuf's words. The command line (`-newgame`, `-arabic`, `-maryam`) is now applied once, when the save is read.
- Windows build after these changes: **69.1 FPS** average over 95 s of the walk, with `-maryam -arabic`, no errors.
  The 60.7 FPS above was the laptop, not the card.
- `npm run unity:export` deletes Unity's `.meta` files in `unity/Assets/Rihla/Content` and `Generated`, and
  `Generated/walk.json`; restore them with git after running it (`git ls-files -d`) until the script is fixed.
- Screenshots: `docs/screenshots/e3d-*.png` (the Jiddo line in `e3d-maryam-feminine-walk-ar.png` was shown on
  demand for the picture).

### Townspeople in 1910 dress (17 November 2026)

- In the Time Lens's "then" view (about 1910) the townspeople wear the dress of that time; when the view turns to
  "now" they change to today's dress, and back again with T. Jiddo, Yusuf and Maryam keep their own clothes.
- The 1910 dress (`FigureBody.Period`), an estimate from what period photographs commonly show, to be checked
  (`NEEDS_REVIEW.md` H18):
  - Men of the town: a red tarboush, some wound with a white turban; a long striped robe and a sash.
  - Men from the villages: a white kufiya and black agal, a white robe and a striped abaya (cloak) open at the front.
  - Men in a European jacket and trousers with a tarboush.
  - Women of the town: a dark wrap from the head to below the waist over a long dark dress.
  - Women from the villages: a long white or indigo thob with a red embroidered chest panel and hem, and a white veil.
  - Older women: the black thob with red tatreez and the white veil, as today.
  Everyone stays covered and faces stay without features (brief section 5).
- Each townsperson keeps both bodies once built and only switches which one is shown, so changing era costs
  nothing (building all 46 again took about 75 ms, a visible stutter).
- Checked in the Editor: everyone is in 1910 dress at the start, in today's dress after the reveal, and they keep
  walking after the switch.
- Windows build: **63.9 and 61.7 FPS** average over 95 s of the walk (the whole run is in the "then" view), no
  errors; within this laptop's usual range (61-69).
- Screenshots: `docs/screenshots/e3e-townspeople-1910.png` and `e3e-townspeople-today.png`, the same eight people
  in each era, stood in a row in front of Bab al-Asbat for the picture (in the game they walk the lanes).

### Children and elders among the townspeople (17 November 2026)

- **9 children** (boys and girls, about two-thirds of a grown-up's height) each walk beside a parent, along the
  lanes and across the courtyard; the parent slows to a child's pace (`Townsfolk.leader`).
  - Today: boys in a shirt and trousers or a thobe and white cap; girls in a long dress and headscarf, as Maryam (the owner chose to keep the headscarf for the girls).
  - About 1910: boys in a long robe with a sash and a tarboush or white cap; girls in a long dress, some
    embroidered, with a white head cloth.
- **10 elders** walk slowly (about half the usual pace): old men with a white or grey beard, a thobe and a kufiya or
  white cap (in 1910 a tarboush with a turban, or a kufiya with an abaya), and the older women in the black thob with
  tatreez. All the old men and more than half of the old women lean on a plain wooden walking stick in the right
  hand, and that arm swings less.
- Now 55 townspeople in all. Who wears what is an estimate (`NEEDS_REVIEW.md` H18).
- Checked in the Editor: children keep beside their parent while walking, elders walk slowly with the stick, and
  both change dress with the Time Lens.
- Windows build: **70.5 and 68.7 FPS** average over 95 s of the walk, no errors.
- Screenshots: `docs/screenshots/e3f-children-elders-1910.png` and `e3f-children-elders-today.png` (four children and
  four elders stood in a row for the picture), and `e3f-child-with-mother-1910.png` (a boy walking beside his mother
  in the courtyard, as in the game).

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
