/**
 * Static-site seam: import the Fable-compiled page module and write it out
 * as plain HTML files — home plus one page per area. The figure gate runs
 * first and fails the build before anything is written. The patient gets
 * static files; F# never ships to them.
 */
import { copyFileSync, mkdirSync, writeFileSync } from 'node:fs';

let loaded = null;
const tried = [];
for (const spec of ['../build/App.js', '../build/src/App.js']) {
  tried.push(spec);
  try {
    loaded = await import(spec);
    console.log(`compiled module found at ${spec} (exports: ${Object.keys(loaded).join(', ')})`);
    break;
  } catch (e) {
    console.log(`not at ${spec} (${e.code ?? e.message})`);
  }
}
for (const fn of ['renderHome', 'renderArea', 'renderGallery', 'areaRoutes', 'validateAll']) {
  if (!loaded || typeof loaded[fn] !== 'function') {
    console.error(`${fn}() not exported; tried: ${tried.join(', ')} (exports: ${loaded ? Object.keys(loaded).join(', ') : 'none'})`);
    process.exit(1);
  }
}

const gate = loaded.validateAll();
console.log(`figure gate: ${gate.Specs} specs for ${gate.Items} items`);
if (gate.Errors.length > 0) {
  console.error(`figure gate FAILED:\n- ${gate.Errors.join('\n- ')}`);
  process.exit(1);
}

const dist = new URL('../dist/', import.meta.url);
mkdirSync(dist, { recursive: true });
writeFileSync(new URL('index.html', dist), loaded.renderHome());
let pages = 1;
for (const id of loaded.areaRoutes()) {
  const dir = new URL(`${id}/`, dist);
  mkdirSync(dir, { recursive: true });
  writeFileSync(new URL('index.html', dir), loaded.renderArea(id));
  pages += 1;
}
// Clinician review gallery (draft, noindexed by content, not a patient route).
const gal = new URL('gallery/', dist);
mkdirSync(gal, { recursive: true });
writeFileSync(new URL('index.html', gal), loaded.renderGallery());
pages += 1;
// Client islands: hand-written, content-free, copied as-is.
for (const js of ['gate.js', 'timers.js', 'done.js', 'share.js', 'textsize.js', 'search.js']) {
  copyFileSync(new URL(`./${js}`, import.meta.url), new URL(js, dist));
}
console.log(`wrote ${pages} pages + 6 islands`);
