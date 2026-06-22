import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    rolldownOptions: {
      onLog(level, log, defaultHandler) {
        const normalizedId = log.id?.replaceAll('\\', '/') || ''
        const isSignalRAnnotationWarning =
          level === 'warn' &&
          log.code === 'INVALID_ANNOTATION' &&
          normalizedId.endsWith('/@microsoft/signalr/dist/esm/Utils.js')

        if (!isSignalRAnnotationWarning) defaultHandler(level, log)
      },
    },
  },
})
