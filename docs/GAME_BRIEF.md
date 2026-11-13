# Prompt for Fable 5.2: "Rihla: Roads Without Borders"

> **How to use this file**
> - **Part A** is the master prompt. Paste all of it into Fable 5.2 at the start of a fresh project.
> - **Part B** holds the follow-up prompts, one per build phase. Send them one at a time, and only once the previous phase works and you have played it.
> - **Part C** is your checklist for reviewing each build, and a list of the things you still need to decide.
>
> Text in `[brackets]` is a placeholder for you to change.

---

## PART A — MASTER PROMPT (paste this first)

```
You are the lead game developer, technical artist, and educational designer for a
new 3D driving game. You will build it yourself, in phases, writing all the code,
running it, testing it in a headless browser, taking screenshots to check your own
visual work, and fixing problems before you report back to me. Work like a small
senior studio: plan first, then build a vertical slice, then expand.

==================================================================
1. THE VISION
==================================================================
Working title: "Rihla: Roads Without Borders" (rihla = journey, in the tradition of
Ibn Battuta's travel writing).

This is a beautiful, relaxing-yet-exciting open-road driving game for children
(ages 7–14) and adults. The mood is "Forza Horizon meets a family travel
documentary". It is NOT a combat or street-racing game. The player drives across
the lands of the late Ottoman era, around 1910 CE / 1328 AH, through historical
Palestine and its neighbours: Egypt, Jordan (Transjordan) and Lebanon. Later
expansions cover Syria, the Hejaz, Anatolia and Iraq.

The core feeling: a family road trip through one connected world with no modern
borders, checkpoints, or passports. Muslims, Christians and Jews live side by side
in the cities. Cultures, markets, food and faith flow along the same roads. The
player should leave the game knowing:
  - the geography: the coastal plain, the hill country, the Jordan Rift Valley,
    the Dead Sea (the lowest point on Earth), the Naqab/Negev and Sinai deserts,
    Wadi Rum, the Lebanon mountains and cedars, the Nile Delta;
  - the cities and what each was known for (Nablus soap, Jaffa oranges,
    Hebron glass and grapes, Gaza's port and markets, Akka's sea walls...);
  - the Islamic and historical significance of each site, told accurately,
    warmly, and at a child's level, with an optional "learn more" layer for adults;
  - respect and adab: good manners on the road, kindness to travellers,
    hospitality, care for the land (no littering, saving water).

==================================================================
2. TECHNOLOGY (default choice; tell me if you strongly disagree)
==================================================================
Build it as a browser game so it can run anywhere and you can test it yourself:
  - TypeScript + Vite
  - Three.js (current stable release) using WebGL2. Design the rendering
    layer so WebGPU can be added later.
  - Rapier (via @dimforge/rapier3d-compat) for vehicle physics, using a raycast
    vehicle controller with tuned suspension, tyre grip, weight transfer and
    surface types (asphalt, cobblestone, packed dirt, sand, gravel, mud).
  - The "postprocessing" library for ACES/AgX tone mapping, bloom, SSAO/GTAO,
    depth of field in photo mode, vignette and colour grading LUTs per region.
  - Howler.js or WebAudio for audio, including an engine sound that follows RPM
    and load.
  - Playwright for automated play-tests and screenshots.
  - Pin exact package versions in package.json.

Graphics target: "stylised realism". Think of the warm, painterly-real look of
modern open-world racing games, but achievable in a browser. Priorities:
  1. Beautiful light: a physically based sky with a sun that moves through a full
     day-night cycle, golden hour and blue hour, stars and a crescent moon at night,
     volumetric-looking fog and haze in valleys, heat shimmer in deserts.
  2. Great terrain: large heightmap terrain built from real elevation data
     (SRTM / Copernicus DEM, which are public), scaled down. Use splat-map
     texturing (limestone, terra rossa soil, sand, scrub, terraced fields) with
     triplanar mapping on slopes.
  3. Dense instanced vegetation with LOD: olive groves, cypress, date palms,
     Lebanon cedars, orange orchards, cactus (sabr) hedges, wheat fields,
     grapevines on terraces.
  4. Detailed hero landmarks (see section 5). Wherever you can, build them in code
     from modular parts: arches, domes, minarets, muqarnas-style details, stone
     courses. Use honest proportions taken from reference.
  5. A cinematic camera: chase cam with speed-based field of view, a bonnet cam,
     an interior cam, a cinematic drone cam on arrival at a landmark, and a full
     photo mode.
  Performance targets: 60 fps on a mid-range laptop, 30 fps on a tablet. Provide
  Low, Medium, High and Ultra quality presets, with automatic detection on first
  launch.

Asset rules: use only assets with CC0 or similarly permissive licences (for
example Poly Haven, ambientCG, Kenney, Quaternius). Keep an ASSETS.md file that
lists the source and licence of every asset. Never use real car brands, logos,
or assets ripped from commercial games. Generate procedurally whatever you can.

==================================================================
3. WORLD DESIGN
==================================================================
Scale: the real distances are far too long for fun driving, so build a
"storybook-compressed" world. Keep the real relative positions, elevations and
landmark order, but compress distances so that a drive between neighbouring
cities takes 3–6 minutes. Regions stream in and out as the player drives.

Launch region (Phase 1–3): HISTORICAL PALESTINE
  Al-Quds (Jerusalem): Al-Masjid Al-Aqsa compound, the Dome of the Rock,
    the Qibli mosque, Ottoman city walls and gates (Bab al-Amud/Damascus Gate,
    Bab al-Khalil/Jaffa Gate), the Mount of Olives viewpoint, the Church of
    the Holy Sepulchre exterior (co-existence theme: the keys held by a Muslim
    family for centuries).
  Al-Khalil (Hebron): Al-Haram al-Ibrahimi, glass-blowing workshops, vineyards.
  Bayt Lahm (Bethlehem), Nablus (Jabal al-Nar, olive-oil soap factories,
    knafeh), Yafa (Jaffa: port, orange groves, the clock tower, built in 1903),
    Akka (Acre: sea walls, Al-Jazzar Mosque, the khans), Haifa and Mount Carmel,
    Gaza (Great Omari Mosque, port, markets), Ramla (the White Tower),
    Tabariyya and Lake Tiberias, Safad, Ariha (Jericho: oasis, date palms),
    Nabi Musa, the Dead Sea, the Naqab desert, Bir al-Saba (Beersheba).

Region 2: TRANSJORDAN
  Amman (Roman theatre), Jerash, the castles of Ajloun and Karak, Petra,
  Wadi Rum, Aqaba, Ma'an and the Hejaz Railway stations (the railway opened in
  1908, so trains should run in the background!), Mu'tah and the shrines of the
  Companions (Ja'far ibn Abi Talib, Zayd ibn Haritha, Abdullah ibn Rawaha), and
  the shrine of Abu Ubaidah ibn al-Jarrah in the Jordan Valley.

Region 3: LEBANON (Mount Lebanon and the coast)
  Beirut, Tripoli (the Mamluk mosques and souks), Sidon/Saida (the Sea Castle and
  Khan al-Franj), Tyre/Sur, Baalbek, the Cedars, Byblos/Jbeil.

Region 4: EGYPT and SINAI
  Al-Arish, the Sinai road, Jabal Musa / St Catherine's (the Fatimid-era mosque
  inside the monastery is a lovely co-existence story), the Suez Canal (opened in
  1869, with ships passing), Cairo (Al-Azhar, Ibn Tulun, the Citadel and the
  Mosque of Muhammad Ali, Khan al-Khalili), the Pyramids of Giza, Alexandria,
  and the Nile Delta.

Expansion regions (design the systems for them now, build them later):
  Syria: Damascus (the Umayyad Mosque), Aleppo citadel, the waterwheels of Hama,
    Palmyra.
  The Hejaz: the Hejaz Railway route to Al-Madinah Al-Munawwarah.
  Anatolia: Istanbul (the Blue Mosque, the Süleymaniye, Hagia Sophia, the
    Bosphorus ferries), Bursa, Konya.
  Iraq: Baghdad, Mosul, Samarra (the spiral minaret).

Road types: Ottoman macadam roads, cobbled city streets, caravan tracks, coastal
roads, mountain switchbacks, desert piste, wadi crossings, fords, and bridges.
Add old khans/caravanserais along the roads as rest stops, garages and save
points.

==================================================================
4. STORY
==================================================================
Premise: [Player character name, either Yusuf or Maryam; let the player choose a
boy or a girl] lives with their grandfather, Jiddo Ibrahim, a kind map-maker in
Al-Quds. Jiddo owns an old, lovingly restored motorcar, a rarity in 1910.
Hidden in the car, they find a torn travel journal: "The Rihla", written by a
great-great-grandmother who travelled these lands centuries ago. Its pages have
been scattered. Some are with families, some with imams, shopkeepers, monks and
teachers along the old roads. Jiddo says: "A road connects hearts before it
connects cities. Let us go and find the pages."

Each journal page found:
  - unlocks a beautifully illustrated, narrated page about the place (history,
    geography, an ayah or hadith where appropriate, a local craft or food);
  - restores part of an animated hand-drawn map of the whole region;
  - opens new roads and regions.

Chapters (one per region) end with a gentle "gathering" scene: a shared meal at a
khan with the friends met on the road, people of different faiths and
backgrounds, which shows hospitality and unity.

Characters met on the road (all respectful and warm, none of them villains):
  a Nablus soap-maker, a Jaffa orange grower, a Hebron glass-blower, a Bedouin
  guide in Wadi Rum, a Hejaz Railway engineer, an Azhar student, a Christian
  shopkeeper in Bethlehem, a Jewish bookbinder in Safad, a Lebanese
  cedar-forester, a Cairo calligrapher, a lighthouse keeper in Alexandria.

The only "antagonists" are nature and time: sandstorms, flash floods in wadis,
winter snow in Al-Quds and Lebanon, a puncture in the desert, running low on
fuel or water, and getting there before maghrib.

==================================================================
5. RELIGIOUS AND CULTURAL GUIDELINES (strict)
==================================================================
  - Never depict, voice, or show the faces of any prophet, the Companions, or
    the family of the Prophet ﷺ. Refer to them in narration only, with proper
    honorifics (ﷺ, عليه السلام, رضي الله عنه).
  - Quran ayat and hadith must be quoted accurately. Show the Arabic text with a
    translation and the reference (surah:ayah, or the hadith collection and
    number). Use only well-known authentic hadith. Mark every religious text in
    a single content file (content/religious_texts.json) so that a scholar can
    review all of it in one place.
  - Sacred spaces: the car can never drive into a mosque, church or shrine
    precinct. The player parks outside and switches to an on-foot "visit" mode:
    a slow walk, a guided camera and narration, shoes off at the threshold.
    Never place race events, stunts or collectibles inside sacred spaces.
  - Adhan: at the five daily prayer times in game time, a soft adhan can be
    heard from the nearest minaret. The player may pull over at a mosque to pray.
    The game gives a small "Barakah" bonus and a calm scene, and never punishes
    the player for not stopping. There is a setting to turn the adhan off and to
    pick reciter-style audio.
  - Music: default to vocal nasheed-style and percussion-only or nature
    soundscapes. Instrumental music is an option the player can turn on.
  - Clothing is modest and true to the period for every character.
  - No gambling, loot boxes, alcohol, romance or violence. No guns. No police
    chases.
  - Historical honesty: be accurate and kind. Gently note real facts such as the
    Ottoman provinces (vilayets and sanjaks), and that Egypt was a nominally
    Ottoman Khedivate under British occupation in 1910, without political
    lecturing. Keep any modern conflict out of the game. The theme is unity,
    knowledge and belonging.
  - Include names in Arabic (with correct right-to-left rendering) and in
    English for every place.

==================================================================
6. GAMEPLAY
==================================================================
Modes:
  - Family Mode (the default for children): steering and braking assist, no
    damage, gentle off-road recovery, a "Jiddo hint" button, larger UI text, and
    an optional route ribbon on the road.
  - Explorer Mode: medium assists, light cosmetic dirt and damage, fuel and
    water management.
  - Simulation Mode (for adults): assists off, a manual gearbox with clutch
    option, realistic tyre and surface physics, and mechanical breakdowns you
    fix at khans.

Activities:
  1. Journey missions (main story): drive from A to B and discover a page.
  2. Deliveries: Nablus soap to Cairo, Jaffa oranges to Beirut, Hebron glass to
     Damascus. Fragile cargo rewards smooth driving.
  3. Friendly challenges, like Forza Horizon "showcase" events but kind: race
     the Hejaz Railway train from Ma'an to the next station; a dune run in Wadi
     Rum; a hill-climb up Mount Carmel; a time trial on the Lebanon switchbacks;
     a "sunset arrival" challenge at Petra.
  4. Discovery collectibles: calligraphy tiles (the 99 Names of Allah, one tile
     at each scenic spot, explained gently for children), Ottoman stamps for the
     journal, the plants and animals of each region (gazelle, ibex, hoopoe, the
     Palestinian sunbird), local foods (maqluba, musakhan, mansaf, knafeh,
     ful, man'oushe).
  5. Quiz stops: optional 3-question quizzes at khans, with friendly feedback,
     in two difficulty levels (child and adult).
  6. Photo mode: free camera, filters, time of day, and an "add to your Rihla
     scrapbook" button.
  7. Garage: restore and customise the car with period-style parts: paint
     colours, wheels, a roof rack for luggage, desert tyres and lamps. There are
     no real brands. Unlock 4–6 fictional cars over time, such as a 1910s
     touring car, a desert truck, a mountain roadster, and a small delivery van.
  8. Character customisation for the child driver, with modest clothing options.

Progression: Barakah points (from kindness: helping a stranded traveller,
giving water, delivering on time) plus Knowledge points (from discoveries and
quizzes). There is no grinding and no paywalls.

Kindness events on the road (random, never mandatory): a traveller with a broken
cart, a shepherd crossing with sheep (wait patiently), an elderly woman who needs
a lift to the market, or a lost kid goat.

==================================================================
7. UI, AUDIO, ACCESSIBILITY
==================================================================
  - Full Arabic and English localisation from day one. The UI must flip for
    right-to-left. Use beautiful fonts: Amiri or Noto Naskh Arabic for Arabic and
    a clean humanist sans for English. Make adding Turkish, Urdu, Malay, French
    and Indonesian easy later.
  - UI style: the journal or illuminated-manuscript aesthetic (parchment,
    geometric Islamic patterns as borders, gold leaf accents). Keep it clean and
    readable, not cluttered.
  - Minimap and full map styled as an Ottoman-era hand-drawn map.
  - Narration: write all voice lines in a script file. Use placeholder TTS now,
    designed so it can be replaced by recorded voice actors later.
  - Audio: engine and tyre sounds that change with the surface, wind, birds,
    markets, waves, call of the adhan, train whistles, camels.
  - Controls: keyboard, gamepad (Gamepad API), and touch controls with tilt
    steering on tablets.
  - Accessibility: subtitles always available, colour-blind-safe UI, reduced
    motion option, remappable keys, adjustable text size, and a "calm mode" with
    no timers.
  - Healthy play: a gentle reminder after 45 minutes, and a parent settings page
    behind a simple gate.
  - Save system: local saves (IndexedDB) with 3 slots and autosave at khans.

==================================================================
8. CODE ARCHITECTURE
==================================================================
  - Clean modules: core (loop, input, settings), render (sky, terrain,
    vegetation, water, post-fx), physics (vehicle), world (streaming, regions,
    roads), content (data-driven JSON for places, quests, dialogue, quizzes,
    religious texts, localisation), ui, audio, save.
  - All educational and story content must be DATA, not code, so that teachers
    and scholars can edit it without programming.
  - Roads: spline-based road generator that projects onto the terrain, with
    junctions, signposts in Ottoman Turkish, Arabic and French (true to the
    period), and cuttings and embankments that blend into the terrain.
  - A debug overlay (FPS, draw calls, memory) and a free-fly dev camera.
  - Write a README with build and run instructions, a DESIGN.md that you keep up
    to date, and a CONTENT_GUIDE.md that explains how to add a new place, quest
    or quiz.

==================================================================
9. HOW YOU MUST WORK
==================================================================
  1. Before writing code, reply with: (a) a short technical design, (b) a phased
     plan with milestones, (c) the risks and your mitigations, (d) any
     questions you have for me. Then wait for my go-ahead.
  2. Build in phases. Each phase must end with a playable build.
  3. After each phase: run the game headless with Playwright, take screenshots
     at sunrise, midday, sunset and night at 2–3 locations, look at them
     critically as an art director, fix what looks cheap or broken, and only
     then report. Include the screenshots in your report.
  4. Measure performance on every quality preset and report the numbers.
  5. Do not invent historical facts. If you are unsure, add the fact to a
     NEEDS_REVIEW.md list instead of guessing.
  6. Keep the scope honest. If a feature would hurt quality or the frame rate,
     say so and propose an alternative.

Start now with step 9.1: the design, the plan, the risks and your questions.
```

---

## PART B — PHASE PROMPTS (send one at a time)

**Phase 1: Vertical slice: "The Road from Al-Quds to Ariha"**
```
Go ahead with Phase 1: one fully polished vertical slice. Build the road from
Al-Quds down through the Judean wilderness to Ariha (Jericho) and the Dead Sea.
Include real elevation data, so that the drop from about 750 m to −430 m is
felt; one hero car with good physics and sound; the full day-night sky;
olive groves and desert scrub; a simplified but beautiful Dome of the Rock and
city walls visible from the Mount of Olives viewpoint; one journal page; one
khan; Family Mode and Simulation Mode; English and Arabic UI. Make this 10
minutes of play look and feel as good as possible before adding anything else.
Report back with screenshots and FPS numbers.
```

**Phase 2: Core systems**
```
Phase 2: add the visit-on-foot mode for sacred sites, adhan and prayer stops,
the journal/scrapbook UI, the quiz system, deliveries, kindness events, the save
system, the gamepad and touch controls, photo mode, and the full content
pipeline (JSON). Move the Phase 1 content into the data files.
```

**Phase 3: Complete Palestine**
```
Phase 3: build out all of historical Palestine from the master prompt (the coast,
the hill country, the Galilee, the Naqab) with every listed city, landmark,
journal page and challenge. Add weather: winter rain, snow in Al-Quds, and
khamsin dust storms. Polish the story chapter and its gathering scene.
```

**Phases 4–6: Transjordan, then Lebanon, then Egypt and Sinai** (one prompt each)
```
Phase [N]: build the [REGION] region exactly as described in the master prompt,
connected seamlessly to the existing world. Add its signature challenge
([race the Hejaz train / the Lebanon switchbacks / crossing the Suez Canal by
ferry]), its new car unlock, its foods, wildlife and quiz set. Keep the frame
rate within budget, and re-check the old regions for regressions.
```

**Phase 7: Polish and release**
```
Phase 7: performance pass, accessibility audit, a full localisation check
(right-to-left layout bugs), parent settings, the onboarding tutorial with Jiddo,
the title screen with a cinematic flyover, and a production build that I can
host as a static website and install as a PWA on tablets.
```

**Useful at any time:**
```
Art-direction pass: take 12 screenshots around the world and critique them as
the art director of a AAA racing game. List the 10 biggest visual weaknesses
and fix the top 5.
```
```
Fun pass: play the first 15 minutes as an 8-year-old and then as a 40-year-old
car enthusiast. What is boring, confusing, or too hard for each? Fix it.
```

---

