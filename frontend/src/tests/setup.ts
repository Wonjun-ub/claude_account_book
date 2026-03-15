import nodeCrypto, { webcrypto } from 'node:crypto'

// Node 16 호환: node:crypto에 getRandomValues 패치 (Vite 내부 사용)
if (!(nodeCrypto as any).getRandomValues) {
  (nodeCrypto as any).getRandomValues = (buf: Uint8Array) => webcrypto.getRandomValues(buf)
}
// globalThis.crypto 폴리필
if (!globalThis.crypto) {
  Object.defineProperty(globalThis, 'crypto', { value: webcrypto })
}
