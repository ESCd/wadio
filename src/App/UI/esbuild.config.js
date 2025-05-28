import { copy } from 'esbuild-plugin-copy';
import esbuild from 'esbuild';
import Yargs from 'yargs';
import { hideBin } from 'yargs/helpers';

const Arguments = Yargs(hideBin(process.argv)).options({
  inputs: {
    demandOption: true,
    requiresArg: true,
    type: 'string'
  },

  output: {
    demandOption: true,
    requiresArg: true,
    type: 'string'
  },

  watch: {
    default: false,
    type: 'boolean',
  }
}).parse();

/** @returns {esbuild.BuildOptions} */
const useConfig = () => ({
  bundle: true,
  define: {
    'process.env.NODE_ENV': '"production"'
  },
  entryPoints: Arguments.inputs.split(';'),
  format: 'esm',
  loader: {
    '.ts': 'ts',
  },
  metafile: true,
  minify: true,
  outdir: Arguments.output,
  plugins: [
    copy({
      assets: [
        { from: ['./node_modules/leaflet/dist/images/*'], to: ['./map'] }
      ]
    })
  ],
  sourcemap: true,
  target: 'es6',
  tsconfig: 'tsconfig.json',
});

if (!Arguments.watch) {
  const result = await esbuild.build(useConfig());
  if (result.errors?.length) {
    console.error(result.errors);
    process.exit(-1);
  } else {
    for (const output in result.metafile.outputs) {
      console.log(output);
    }
  }
} else {
  await esbuild.context(useConfig()).then(context => context.watch());
}