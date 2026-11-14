# Moving Rihla to Unity: setting up a cloud graphics PC

The owner's laptop has older built-in Intel graphics, and the cloud session that built the browser game has no
graphics card at all. A game engine needs a real graphics card, so the plan is to **rent a Windows PC with a
graphics card in the cloud**, open it from the laptop like a video call, and run Unity and Claude Code on it.

## 1. The options

Prices are from November 2026 reviews and differ by region and promotion; check the provider's own page.

| Option | What it is | Cost | Good | Not so good |
|---|---|---|---|---|
| **Shadow PC** (recommended to start) | A Windows PC in the cloud, opened with Shadow's app from the laptop, a tablet or a phone | About $30–38 a month for the entry plans (NVIDIA RTX 2000 Ada or RTX 4060-class, 16 GB graphics memory); about $55–60 for the Power plans | Simplest; a fixed monthly price; feels like a normal Windows PC | Monthly hour limits (120 hours on Lite plans, 210 on Neo and Power) and session limits of 4–8 hours; the PC switches off after you disconnect |
| **Amazon (AWS) cloud workstation** | A Windows server with an NVIDIA graphics card, opened with Amazon's free DCV viewer | About $0.80–1.00 an hour for a g6 or g5 machine, plus Windows and storage; about $100–200 a month at 4 hours a day | Pay only for hours used; can run for long jobs without you | Much more setup (an AWS account, security settings, remembering to switch it off) |
| **Buy or borrow a PC with a graphics card** | A desktop or gaming laptop at home | One-off cost | No monthly fee, no limits | Up-front cost |

Not recommended: a university research computer. Its terms almost certainly do not cover personal projects.

**Recommendation:** start with **Shadow PC on a Neo plan**. It is the quickest way to a working setup. If the hour
limits get in the way, move to AWS later; nothing in the project depends on the provider.

## 2. Setting up the cloud PC (about an hour, once)

Do these steps on the cloud PC itself, after opening it from your laptop.

1. **Sign up for Shadow PC** at shadow.tech, choose a plan, install the Shadow app on your laptop and open your
   cloud PC. Windows starts like a normal computer.
2. **Install the tools.** Open *Windows PowerShell* and run these commands one at a time. They use Windows' own
   installer, `winget`. *(Not yet tested on a Shadow PC; tell Claude if one fails.)*
   ```powershell
   winget install --id Git.Git -e
   ```
   ```powershell
   winget install --id Unity.UnityHub -e
   ```
   ```powershell
   winget install --id OpenJS.NodeJS.LTS -e
   ```
3. **Install Unity 6.** Open *Unity Hub*, sign in with a free Unity account, choose a **Unity Personal** licence,
   then *Installs → Install Editor →* the newest **Unity 6 LTS**, with the modules **Android Build Support** and
   **Windows Build Support (IL2CPP)**. *(iPad builds need a Mac; that comes later.)*
4. **Install the Claude desktop app** from claude.ai/download and sign in with your Claude account.
5. **Get the project.** In PowerShell:
   ```powershell
   git clone https://github.com/amelsheikh/Rihla-Islamic-Game.git C:\Projects\Rihla
   ```
   GitHub will ask you to sign in, because the project is private.
6. **Start Claude Code in the project.** In the Claude desktop app, open *Claude Code*, choose the folder
   `C:\Projects\Rihla`, and send:
   > Read CLAUDE.md, docs/STORY.md, docs/ENGINE_SETUP.md and docs/PROGRESS.md, then start Phase E0.

From then on, Claude works on the cloud PC: it can open Unity, build, play the game and take screenshots, and you
watch and play on the same screen.

## 3. What Unity adds that the browser could not

- **Real cities as they are today, from the air.** *Cesium for Unity* (a free plugin) streams Google's
  photorealistic 3D map of the world, so the children can fly over today's cities. It needs a Google Maps API key
  with billing, must show Google's credits on screen, and its terms must be checked before the game is shared or
  sold. Whether Al-Quds and the other places in the game are covered in 3D, and how well, still has to be checked.
  This map looks real from the air but blurry at street level, so it is used for flying, not for walking.
- **Places to walk, built properly.** The places the story visits on foot (the Old City, al-Masjid al-Aqsa,
  Ariha, the khans) are built as detailed scenes, each with a "then" and a "now" version.
- **Life:** people walking, shops, market stalls and traffic, from Unity's asset store. Assets that look like
  this region are scarce, so some will need to be made or commissioned.
- **Graphics:** realistic lighting, trees and materials, and better cars.
- **Tablets:** the same project builds for Android tablets with lighter settings.

## 4. Phase E0 (the first job on the cloud PC)

1. Create a Unity 6 project in `unity/` in this repository, using the Universal Render Pipeline.
2. Add the Unity command-line tool's Pipeline package, so Claude can drive the editor.
3. Export the existing terrain and the Jericho road from `tools/build-terrain.ts` in a form Unity reads, and
   build them as a Unity scene.
4. Bring in the content files in `content/` as they are (places, history, journal, religious texts, both languages).
5. A placeholder car that drives the road, and the child walking.
6. Install Cesium for Unity and test flying over Al-Quds (needs the owner's Google Maps API key).
7. Screenshots and a Windows build; commit and push.

## 5. Costs to expect

- The cloud PC: about $30–60 a month.
- A Google Maps Platform account for the 3D map: free usage up to a limit, then paid; check Google's current pricing.
- Assets from the Unity Asset Store (cars, people, shops, trees): roughly $50–300 in total to start, chosen together.
- Unity Personal: free while revenue stays under Unity's limit.

## Sources

- Shadow PC prices and specifications: itechguides.com/products/shadow-pc, frontdeskreview.com/software/cloud-gaming/shadow-pc,
  clouddosage.com (Shadow PC Lite launch), tech-insider.org (Shadow PC hour caps)
- AWS g5 and g6 prices: aws.amazon.com/ec2/pricing/on-demand, economize.cloud (g6 family), wring.co (AWS GPU pricing)
- Cesium for Unity and Google 3D tiles: cesium.com/learn/unity/unity-photorealistic-3d-tiles,
  developers.google.com/maps/documentation/tile/3d-tiles, cesium.com/learn/ion/content-usage-and-attribution-guide
