export default ctx => ({
  map: ctx.options.map,
  parser: ctx.options.parser,
  plugins: {
    'postcss-prepend-imports': {
      path: './',
      files: ['_Imports.css']
    },
    '@tailwindcss/postcss': {},
    'postcss-preset-env': {
      env: ctx.env
    },
    cssnano: ctx.env === 'production' ? {} : false,
  }
})