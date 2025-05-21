import esbuild from 'esbuild';

const Interop = name => ({
  in: `Interop/${name}Interop.ts`,
  out: `${name}.module`
});

const useConfig = () => ({
  bundle: true,
  define: {
    'process.env.NODE_ENV': '"production"'
  },
  entryPoints: [
    Interop('LocalStorage'),
    Interop('Player')
  ],
  format: 'esm',
  loader: {
    '.ts': 'ts',
  },
  minify: true,
  outdir: 'wwwroot/Interop',
  sourcemap: true,
  target: 'es6',
  tsconfig: 'tsconfig.json',
});

await (!process.argv.includes('--watch')
  ? esbuild.build(useConfig())
  : esbuild.context(useConfig()).then(context => context.watch()));

