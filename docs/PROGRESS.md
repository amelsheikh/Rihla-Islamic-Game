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

## Direction

During Phase 1b the owner asked for the game to look like Forza Horizon, with real cars and realistic scenes.
That cannot be reached by continuing this browser build alone. The options put to the owner are recorded in
`DESIGN.md` section 10; no further phase should start until the owner chooses.

## Still open with the owner

- The direction above.
- Who supplies or approves adhan recordings, and who reviews religious texts and the Arabic.
