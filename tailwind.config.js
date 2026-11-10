/** @type {import('tailwindcss').Config} */
const UTILITY_COLORS = 'blue|amber|emerald|rose|indigo|sky|orange|red|slate';
const SHADES = '50|100|200|300|400|500|600|700';

export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {},
  },
  safelist: [
    // Components build class names like `bg-${color}-600` at runtime, so Tailwind must generate every combination.
    {
      pattern: new RegExp(`^(bg|text|border|ring)-(${UTILITY_COLORS})-(${SHADES})$`),
      variants: ['hover'],
    },
    {
      pattern: new RegExp(`^bg-(${UTILITY_COLORS})-50/50$`),
      variants: ['hover'],
    },
  ],
  plugins: [],
};
