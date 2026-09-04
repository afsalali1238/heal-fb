/**
 * Static-site seam: import the Fable-compiled page module and write it out
 * as plain HTML. The patient gets a static file; F# never ships to them.
 */
import { mkdirSync, writeFileSync } from 'node:fs';

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
if (!loaded || typeof loaded.renderPage !== 'function') {
  console.error(`renderPage() not exported; tried: ${tried.join(', ')}`);
  process.exit(1);
}
mkdirSync(new URL('../dist/', import.meta.url), { recursive: true });
const html = loaded.renderPage();
writeFileSync(new URL('../dist/index.html', import.meta.url), html);
console.log(`wrote dist/index.html (${html.length} chars)`);
