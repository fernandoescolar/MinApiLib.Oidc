import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The backend renders the HTML: an index with <div id="app" app-id="..." app-model="..."> and the script and style
// of this build, which it reads from wwwroot/.vite/manifest.json. So the build has no index.html, only the entry.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
    manifest: true,
    rollupOptions: {
      input: 'src/main.tsx',
    },
  },
});
