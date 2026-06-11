/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#16151A",
        paper: "#F5F6F8",
        card: "#FFFFFF",
        accent: "#5B5BD6",
        "accent-soft": "#ECECFB",
        ember: "#E2683C",
        sage: "#3F9D78",
        muted: "#6B6B76",
        line: "#E4E4EA",
      },
      fontFamily: {
        display: ['"Spectral"', "Georgia", "serif"],
        sans: ['"Inter"', "system-ui", "sans-serif"],
        mono: ['"JetBrains Mono"', "ui-monospace", "monospace"],
      },
    },
  },
  plugins: [],
};
