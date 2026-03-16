import { defineConfig } from '@playwright/test'

// 갤럭시 S25 (SM-S931B) 기기 프로파일
const GALAXY_S25 = {
  viewport: { width: 360, height: 780 },
  userAgent:
    'Mozilla/5.0 (Linux; Android 14; SM-S931B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36',
  hasTouch: true,
  isMobile: true,
}

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  expect: { timeout: 8_000 },

  // 테스트 격리: DB 상태 의존 관계가 있으므로 직렬 실행
  fullyParallel: false,
  workers: 1,
  retries: 0,

  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],

  use: {
    baseURL: 'http://localhost:5173',
    ...GALAXY_S25,
    // 실패 시 디버깅 자료 자동 저장
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    video: 'off',
  },

  projects: [
    {
      name: 'galaxy-s25',
      use: { ...GALAXY_S25 },
    },
  ],
})
