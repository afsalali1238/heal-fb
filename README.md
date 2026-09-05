# physio-fable — Patient Library rebuilt with Fable (F#)

A from-scratch rebuild of the physiotherapy Patient Library where **all
domain logic, safety rules, content, and illustration geometry are written
in F#** and compiled to JavaScript with [Fable](https://fable.io). A tiny
Node seam renders the compiled modules to **static HTML** — patients receive
plain files, F# never ships to a browser.

Isolated from `anatomy-explorer/` (the Astro app) on purpose: this is a
separate line of work that must earn its place before anything merges.

## Status: vertical slice 1

- `src/Domain.fs` — content model (areas, items, dose, draft/published status)
- `src/Safety.fs` — red-flag triggers + pure gate decision (`decide`)
- `src/Content.fs` — seed content: neck area, 2 items (both `Draft`)
- `src/Figures.fs` — deterministic SVG pose renderer driven by joint angles
  (2D kinematic chains; same angles = same picture, always)
- `src/App.fs` — assembles one static demo page (banner, gate draft,
  start-slowly callout, 2 item cards with figures)
- `scripts/render.mjs` — writes `dist/index.html` from the compiled module

## Prerequisites

- .NET 8 SDK (`dotnet --version` → 8.x)
- Node 22+

## Build

```sh
npm install
dotnet tool install --global fable   # once per machine; latest stable, deliberately unpinned
npm run build   # fable Client.fsproj --outDir build && node scripts/render.mjs
```

Without the Fable toolchain, `npm run build` keeps the committed `dist/`
untouched — useful on machines (like Vercel's build image) without .NET.

Output: `dist/` — home plus one file per route. Open any page directly,
no server needed.

CI (`.github/workflows/build.yml`) does exactly the above on every push
and pull request, runs the Playwright QA pass, and uploads `dist/` as an
artifact. On pushes it also commits a freshly built `dist/` whenever the
tracked one is stale (with `[skip ci]`), so the committed output can never
silently disagree with the F# source (see "Deploying" below).

## Deploying (Vercel)

Vercel's build image has no .NET SDK, so the F# compile cannot run there.
That is fine: this project's rule is that patients receive plain files and
F# never ships to a browser. So `dist/` — the rendered static site — is
committed, and Vercel serves it verbatim (`vercel.json` points the output
at `dist/`, no build command).

1. Import this repository at [vercel.com](https://vercel.com) → New Project.
2. The defaults work: `vercel.json` supplies the output directory, so the
   framework preset, build command, and install command can all stay
   empty/unset. **Root Directory must stay empty** — this repository is
   standalone; the project root is the site root.
3. Deploy. Every push to `main` redeploys the committed `dist/`.

If the project was imported earlier from the monorepo layout, clear the
saved Root Directory (Settings → General) and set the output directory to
`dist` — otherwise deploys look for a directory that no longer exists
(this project's was `anatomy-explorer`, the old Astro app folder). After
changing settings, push a new commit: redeploying an old deployment can
replay that deployment's original settings.

Workflow when content or figures change: edit the F# (or islands), push.
CI rebuilds from source and commits the refreshed `dist/` itself, which
triggers the next Vercel deploy — no local .NET install required. If you
do have the toolchain locally, `npm run build` reproduces the same files.

The draft build ships with `robots.txt` blocking crawlers and a `noindex`
meta tag on every page — deliberate, since nothing here is clinically
reviewed yet. Remove both when the library is published for real.

## Architecture rules (carry over from the rebuild brief)

1. F# owns decisions and pictures; JS/HTML owns nothing but delivery.
2. Static first: every slice must render to a file a patient can open.
3. No backend, no accounts, no analytics, no patient-data transmission.
4. Nothing `Published` without a named clinician reviewer — everything here
   is `Draft` until that happens.
5. Pictures are joint-angle schematics, never AI photoreal bodies.

## Roadmap

- Slice 2 (landed): all 8 areas + 21 items; per-item start→end pose specs
  with auto movement arrows; figure validation gate in F# that runs before
  every render (missing/orphan specs, angle ranges, bounds, arrow length).
- Slice 3 (landed): routing — home plus one static file per area;
  thumbnails drawn from the same figure geometry; first client island
  (safety gate: F#-rendered data attributes upgraded by hand-written JS,
  honest static list without JS).
- Slice 4 (landed): hold timers, device-local completion marks, text-size
  control, client search over an F#-authored index, share-link buttons,
  clinician review gallery. Islands are hand-written JS driven by
  F#-rendered data attributes — no dead controls without JS.
- Slice 5 (landed): per-area education notes, legal notice, 404 with a way
  home, dedicated gate-first locator route (blocking island mode), home as
  a funnel, zero-empty-slots assertion on built output.

Remaining (needs humans, not more code): clinician review + countersign of
all content and figures; a canonical public domain (unlocks QR codes,
sitemap, absolute share URLs); real-browser pass incl. dark mode and no-JS
run-through on actual devices; deciding this line's future vs the Astro
app (merge, replace, or retire one).
