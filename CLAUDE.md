# Rihla: Roads Without Borders — standing instructions

- The full game brief is `docs/GAME_BRIEF.md`. Part A is the master brief; Part B lists the phases.
- `docs/OWNER_GUIDE.md` is the owner's own checklist; read it for context, but don't treat it as tasks.
- At the start of every session read `docs/GAME_BRIEF.md`, then `DESIGN.md` and `NEEDS_REVIEW.md` if they exist, then `docs/ROADMAP.md` (the objectives and the plan ahead) and `docs/PROGRESS.md` to see which phase we're on. Keep the roadmap's status column current.
- Do only the phase the owner asks for. End every phase with a playable build, screenshots and FPS numbers, as section 9 of the brief describes.
- Keep `docs/PROGRESS.md` updated: the phase completed, what works, known issues, and what comes next.
- Commit with clear messages and push before ending a session; cloud sessions are temporary.
- Religious and cultural guidelines in section 5 of the brief are strict and never optional.
- Never invent historical or religious facts; add uncertain ones to `NEEDS_REVIEW.md`.
- **Direction (current):** the game is moving to **Unity 6**, run on a rented cloud Windows PC with a graphics card, because the browser build reached its limit. Read `docs/ENGINE_SETUP.md` (setup and Phase E0) and `docs/STORY.md` (the story and the owner's decisions in section 8) before doing anything. The owner's purpose is to teach their children about their historical home and Islamic history, so teaching content comes first. Show every place both as it was and as it is now.
- The browser build (`src/`, `index.html`) stays as the prototype for story and teaching content; do not delete it.
- Free assets come from Poly Haven (CC0); record every asset, free or bought, in `ASSETS.md`.
- History content lives in `content/history.json`; list every date and claim in `NEEDS_REVIEW.md` until checked.
- Reference works for checking history and places are listed in `docs/SOURCES.md` (among them the Palestinica the owner named).
- Shared code lives in `unity/Packages/com.rihla.core` (assemblies Rihla.Core.Runtime and Rihla.Core.Editor), used by Road to AlQuds through a git URL; per-game paths come from `CoreConfig`. Tag each release `core-vX.Y.Z`.
