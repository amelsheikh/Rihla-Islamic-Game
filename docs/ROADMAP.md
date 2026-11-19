# Rihla: the roadmap

The single page to check before each session: why we are building this, where we are, and what comes next.
Details of finished work are in `docs/PROGRESS.md`; the story is in `docs/STORY.md`.
*Last revised 19 November 2026.*

## 1. Why we are building it (the main objectives)

In the owner's words: *to teach my children, explore with them my historical home, and connect them to their roots
and Islamic history*, in a game *so beautiful and realistic that it feels like visiting*.

Every piece of work should serve at least one of these, in this order:

1. **Teach truthfully.** Real places, real history and Islamic history, with every fact and text checked
   (`NEEDS_REVIEW.md`). Teaching comes before spectacle.
2. **Then and now.** At every place the children see how it was, then how it is today (the Time Lens).
3. **Feel like visiting.** Walking real streets at true scale, with light, stone, people, sound and emotion.
4. **A journey with family.** The story of Jiddo, Teta and the child following Zaynab's pages from place to
   place, on foot, by car and from the air.
5. **Respect, always.** The religious and cultural rules of the brief (section 5) are never optional.

## 2. Where we are (19 November 2026)

| Done | What it gives |
|---|---|
| Browser prototype (`src/`, kept) | The first story and teaching content; the drive from Al-Quds to Ariha |
| E0: Unity 6 project | Terrain, road and car; flying over today's Al-Quds on Google's 3D map |
| E1: teaching systems | Arabic and English interface, banners, the first journal page, history views, saving |
| E2: walking Al-Quds | The walk from Bab al-Asbat to al-Masjid al-Aqsa at true scale, three clues, the first Time Lens reveal |
| E3: realism on the laptop, three passes | Stone, arches, doors, shops, roofs, light, sound, 55 townspeople in modest dress (1910 and today), choosing Yusuf or Maryam |

The walk is about **10 minutes of play**. Chapter 1 as written in `docs/STORY.md` needs about 45 to 60.

**The laptop's limit:** its Intel HD graphics run the walk at about 60-70 FPS. Realism on it means smart, cheap
detail (textures, objects, sky, colour), not heavy lighting. A graphics PC (rented or bought) is still the way to
the last big step in realism; nothing in the project has to change to move to one.

## 3. The plan ahead

Each step ends with a build the owner can play, screenshots and FPS numbers.

| Step | What | Why now | Status |
|---|---|---|---|
| **R4** | **"As if visiting": realism on the laptop.** A real sky with clouds, weathered and varied stone, life in the lanes (lamps, plants, awnings, arches across lanes), better trees; keep 60 FPS. A prototype the owner can test. | The owner asked for it (19 November) | Done 19 November (63-66 FPS); the owner is testing the prototype |
| **R4b** | **Realism, next steps on the laptop** (if the owner wants more before C1a): varied building heights and roof lines, the slope of the Old City, market goods in the souq, better lamps and awnings | Most visible weaknesses left after R4 | Proposed |
| **C1a** | **The prologue complete.** Jiddo's house, the chest and Page 1 with its riddle; the adhan and the prayer moment (waits on the owner's and a scholar's decision on how prayer is shown) | Finishes the first teaching scene end to end | Next |
| **C1b** | **The road down.** The drive from Al-Quds to Ariha in Unity on the real terrain, Jiddo's remarks on the land, Khan al-Ahmar, the shepherd, the sea-level marker | The land itself is half of the teaching | |
| **C1c** | **Ariha, Maqam al-Nabi Musa and the Dead Sea.** Each walked on foot with clues, a keeper, a question, a page, then and now; Teta's call; the finale | Completes Chapter 1 (45-60 minutes) | |
| **T1** | **Ways to travel.** Horse and carriage in "then" scenes, bicycle and car today, glider and plane over the land | The owner's vision; best added once Chapter 1 has places to travel between | |
| **G1** | **Graphics PC step.** Higher settings, baked light, detailed characters and landmarks | When a graphics PC is available | Waiting |
| **C2, C3...** | **The rest of Palestine**, then Jordan, Lebanon, Egypt and Syria (`docs/STORY.md` section 8) | | |

The browser phases 1b to 7 in `docs/PROGRESS.md` are replaced by this plan. The earlier E4 (ways to travel) is
now T1, after Chapter 1, so that teaching comes first; the owner can reorder.

## 4. Waiting on the owner

- How prayer is shown (the child prays; a scholar to advise on depicting it).
- How present-day places are shown, and whether any place is left out for now (`docs/STORY.md` section 8, item 8).
- Which sources to use for the "then" views (old photographs, drawings).
- Who reviews the religious texts, the history and the Arabic (`NEEDS_REVIEW.md`).
- Whether to rent or buy a graphics PC, and when.

## 5. Reviews needed before anyone outside the family plays

`NEEDS_REVIEW.md` lists every date, claim and text still to check: 18 historical items, the feminine Arabic (L1),
and 8 religious items. None is treated as settled.
