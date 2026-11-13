import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

// "single" mode produces one self-contained HTML file that opens by double-click, for private review.
export default defineConfig(({ mode }) => ({
  base: './',
  assetsInclude: ['**/*.rhm'],
  plugins: mode === 'single' ? [viteSingleFile()] : [],
  build: {
    target: 'es2022',
    outDir: mode === 'single' ? 'dist-single' : 'dist',
    assetsInlineLimit: mode === 'single' ? 100_000_000 : 4096,
    chunkSizeWarningLimit: 20000,
  },
  test: { include: ['tests/**/*.test.ts'], testTimeout: 180000 },
}));
