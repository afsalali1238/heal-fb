/**
 * Build entry: `npm run build`.
 *
 * With the Fable toolchain installed this compiles the F# sources and
 * regenerates dist/ from scratch (what CI does on every push). Without it
 * — e.g. on Vercel, whose build image has no .NET SDK — it keeps the
 * committed dist/ untouched. The committed output is the deploy source of
 * truth, and CI guarantees it always matches the F# source.
 */
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const distFresh = existsSync(fileURLToPath(new URL('../dist/index.html', import.meta.url)));
const shell = process.platform === 'win32';

const probe = spawnSync('fable', ['--version'], { encoding: 'utf8', shell });
if (probe.status !== 0) {
  if (distFresh) {
    console.log('fable not on PATH — keeping the committed dist/ (deploy source of truth).');
    process.exit(0);
  }
  console.error(
    'fable not on PATH and no committed dist/ found.\n'
      + 'Install the .NET 8 SDK, then: dotnet tool install --global fable',
  );
  process.exit(1);
}

const run = (cmd, args) => {
  const r = spawnSync(cmd, args, { stdio: 'inherit', cwd: root, shell });
  if (r.status !== 0) process.exit(r.status ?? 1);
};
run('fable', ['Client.fsproj', '--outDir', 'build']);
run(process.execPath, ['scripts/render.mjs']);
