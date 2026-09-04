/**
 * Static-site seam: import the Fable-compiled page module and write it out
 * as plain HTML. The patient gets a static file; F# never ships to them.
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { renderPage } from '../build/App.js';

mkdirSync(new URL('../dist/', import.meta.url), { recursive: true });
const html = renderPage();
writeFileSync(new URL('../dist/index.html', import.meta.url), html);
console.log(`wrote dist/index.html (${html.length} chars)`);
