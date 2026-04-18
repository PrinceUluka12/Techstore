/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx,ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        display: ['"Clash Display"', '"DM Sans"', 'sans-serif'],
        body: ['"DM Sans"', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'monospace'],
      },
      colors: {
        brand: {
          50:  '#f0f4ff',
          100: '#e0e9ff',
          200: '#c0d0ff',
          300: '#91aaff',
          400: '#5b7dff',
          500: '#2d52ff',
          600: '#1030f5',
          700: '#0d24d6',
          800: '#1120a8',
          900: '#121e84',
          950: '#0b1154',
        },
        surface: {
          50:  '#fafafa',
          100: '#f4f4f5',
          200: '#e4e4e7',
          300: '#d4d4d8',
          800: '#27272a',
          900: '#18181b',
          950: '#09090b',
        }
      },
      animation: {
        'fade-up':   'fadeUp 0.5s ease forwards',
        'fade-in':   'fadeIn 0.4s ease forwards',
        'slide-in':  'slideIn 0.3s ease forwards',
        'pulse-slow': 'pulse 3s ease-in-out infinite',
      },
      keyframes: {
        fadeUp:  { from: { opacity: 0, transform: 'translateY(16px)' }, to: { opacity: 1, transform: 'translateY(0)' } },
        fadeIn:  { from: { opacity: 0 }, to: { opacity: 1 } },
        slideIn: { from: { transform: 'translateX(-12px)', opacity: 0 }, to: { transform: 'translateX(0)', opacity: 1 } },
      },
      boxShadow: {
        'card':  '0 1px 3px rgba(0,0,0,.06), 0 4px 16px rgba(0,0,0,.06)',
        'card-hover': '0 4px 24px rgba(0,0,0,.12)',
        'glow':  '0 0 32px rgba(45,82,255,.25)',
      }
    }
  },
  plugins: []
}
