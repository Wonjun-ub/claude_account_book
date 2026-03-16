# BudgetTracker

개인 가계부 웹앱 (MVP) — .NET 8 + Vue3 + Supabase PostgreSQL

## 저장소

- **원격 저장소**: [https://github.com/Wonjun-ub/claude_account_book.git](https://github.com/Wonjun-ub/claude_account_book.git)

## 기술 스택

| 영역 | 기술 | 버전 | 선택 근거 |
|------|------|------|----------|
| 프론트엔드 | Vue3 + Vite + TypeScript | Vue 3.4 / Vite 4.x | .NET 8과의 조합으로 타입 안정성과 빠른 개발 속도 확보 |
| 런타임 | Node.js | **16.x (고정)** | 인프라 호환성 및 환경 안정성 — 서버 환경이 Node 16에 고정되어 있으므로 모든 의존성은 Node 16 호환 버전을 사용해야 함 |
| CSS | Tailwind CSS | 3.x | |
| 차트 | Chart.js | 4.x | |
| 날짜 처리 | Day.js | 1.x | |
| 백엔드 | .NET 8.0 Web API (C#) | 8.x | Vue3 TypeScript와의 조합으로 프론트-백 전 계층 타입 안전성 달성 |
| 데이터베이스 | Supabase PostgreSQL | — | Node 16 제약 환경에서도 REST API 기반으로 서버리스 기능을 원활히 활용 가능 |
| ORM | Entity Framework Core | 8.x | |
| 상태관리 | Pinia | 2.x | |
| 패키지 매니저 | npm | — | |
| 배포 | Render | — | 프론트 + 백엔드 통합 |

### Node 16 호환성 제약

> **모든 npm 패키지는 Node 16.x에서 동작하는 버전이어야 합니다.**

- `frontend/package.json`의 `engines.node`는 `"16.x"`로 고정
- Node 18+ 전용 패키지(예: cross-env v10+) 사용 금지 → Node 16 호환 버전으로 고정
- CI(`ci.yml`)도 `node-version: "16"` 사용

### 의존성 버전 관리 원칙

- **Node 16 호환성 영향이 큰 패키지는 Fixed Version(`x.y.z`) 사용** — caret(`^`), tilde(`~`) 범위 지정 시 Node 16 비호환 버전으로 자동 업그레이드될 위험이 있음
- 런타임 의존성(`dependencies`)은 특히 버전 고정 권장
- devDependency 추가 시 해당 패키지의 Node 엔진 요구사항 확인 필수 (`npm info <pkg> engines`)

## 언어 및 커뮤니케이션 규칙

- 기본 응답 언어: 한국어
- 코드 주석: 한국어로 작성
- 커밋 메시지: 한국어로 작성
- 문서화: 한국어로 작성
- 변수명/함수명: 영어 (코드 표준 준수)

## Git 브랜치 전략

### Sprint 흐름 (기능 개발)

```
sprint{n}  →  PR to develop  →  로컬 직접 실행 검증  →  PR to main  →  서버 자동 배포
```

### Hotfix 흐름 (긴급 패치)

```
hotfix/*  →  PR to main  →  서버 자동 배포  →  main을 develop에 역머지
```

- `sprint{n}`: 스프린트 단위 개발 브랜치
- `develop`: 스테이징 통합 브랜치 (로컬 직접 실행 검증)
- `main`: 프로덕션 브랜치 (GitHub Actions → 서버 자동 배포)
- `hotfix/*`: 긴급 운영 패치 (main 기반 분기, main PR 후 develop 역머지)

자세한 CI/CD 정책은 `docs/ci-policy.md` 참조. 개발 프로세스 전체는 `docs/dev-process.md` 참조.

## Bash 명령 실행 규칙

- Bash 명령 실행 시 `cd /path &&` 접두사를 사용하지 마세요. 작업 디렉토리가 이미 프로젝트 루트로 설정되어 있습니다.
- 특히 git 명령은 반드시 `git ...` 형태로 직접 실행하세요. (`cd ... && git ...` 금지)

## 개발시 유의해야할 사항

- **plan 모드에서 수정사항을 받으면 반드시 Hotfix vs Sprint 의사결정을 먼저 수행합니다:**

  1. 수정사항의 긴급도, 변경 범위, DB 변경 여부, 의존성 추가 여부를 분석합니다.
  2. 아래 기준에 따라 Hotfix 또는 Sprint를 추천합니다.
  3. 사용자의 최종 결정을 받은 후 해당 프로세스를 따릅니다.

  **Hotfix 추천 기준** (모두 충족 시):

  - 프로덕션 장애/버그이거나, 변경 범위가 파일 3개 이하 & 코드 50줄 이하
  - DB 스키마 변경 없음
  - 새 의존성(pip/npm) 추가 없음

  **Sprint 추천 기준** (하나라도 해당 시):

  - 새 기능 추가 또는 여러 모듈에 걸친 작업
  - DB 스키마 변경 필요
  - 새 의존성 추가 필요
  - 파일 4개 이상 또는 코드 50줄 초과 변경

- sprint 관련 문서 구조:
  - 스프린트 계획/완료 문서: `docs/sprint/sprint{n}.md`
  - 스프린트 첨부 파일 (스크린샷, 보고서 등): `docs/sprint/sprint{n}/`
- sprint 개발이 plan 모드로 진행될 때는 다음을 꼭 준수합니다.

  - karpathy-guidelines skill을 준수하세요.
  - sprint 가 새로 시작될 때는 새로 branch를 sprint{n} 이름으로 생성하고 해당 브랜치에서 작업해주세요. (worktree 사용하지 말아주세요)
  - 다음과 같이 agent를 활용합니다.
    1. sprint-planner agent가 계획 수립 작업을 수행하도록 해주세요.
    2. 구현/검증 단계에서는 각 task의 내용에 따라 적절한 agent가 있는지 확인 한 후 적극 활용해주세요.
    3. 스프린트 구현이 완료되면 sprint-close agent를 사용하여 마무리 작업(ROADMAP 업데이트, PR 생성, 코드 리뷰, 자동 검증)을 수행해주세요.
    4. sprint-close agent는 **`develop` 브랜치로 PR**을 생성합니다. (main이 아닌 develop)
    5. `develop` → `main` merge는 별도 QA 통과 후 deploy-prod agent를 사용합니다.

- hotfix 개발이 plan 모드로 진행될 때는 다음을 꼭 준수합니다.

  - karpathy-guidelines skill을 준수하세요.
  - `main` 기반으로 `hotfix/{설명}` 브랜치를 생성합니다. (worktree 사용하지 말아주세요)
  - sprint-planner agent는 사용하지 않습니다. (계획 수립 불필요)
  - 구현 완료 후 hotfix-close agent를 사용하여 마무리 작업(PR to main, 경량 검증, deploy.md 기록, develop 역머지 안내)을 수행합니다.
  - ROADMAP.md 업데이트나 sprint 문서 작성은 불필요합니다.
  - 프로덕션 배포는 main merge 시 GitHub Actions가 자동 수행합니다.
  - 배포 후 실서버 검증이 필요하면 deploy-prod agent의 5단계(실서버 자동 검증)를 참조합니다.

## 목업 우선 개발 원칙 (Mockup-First)

**모든 기능 변경은 반드시 목업 확인을 거친 후 실서비스(프론트엔드·백엔드)에 반영합니다.**
이 원칙은 예외 없이 적용됩니다. 요구사항이 불명확한 상태에서 실서비스 코드를 수정하지 않습니다.

### 핵심 규칙

1. **신규 기능**: MockupView에 먼저 구현 → 로컬에서 UI/UX 확인 → 확정 후 실서비스 이관
2. **기존 기능 변경**: 변경될 UI를 MockupView에 먼저 반영 → 확인 → 실서비스 수정
3. **백엔드 변경**: API 설계는 목업 확인 후 확정. DB 스키마·API 변경은 목업 단계 이후에만 진행
4. **목업 미확인 상태에서 실서비스 페이지(`HomeView`, `StatsView`, `SettingsView` 등) 수정 금지**

### 목업 페이지 스펙

| 항목 | 내용 |
|------|------|
| 경로 | `/mock-up` |
| 접근 환경 | 로컬 개발(`vite dev`)에서만 접근 가능 |
| 배포 포함 여부 | **미포함** — production 빌드 시 번들에서 완전 제외 |
| Git 커밋 | **포함** — 팀 전체가 로컬에서 확인 가능해야 함 |
| 백엔드 연동 | **없음** — `frontend/src/mocks/` 의 mock 데이터로만 동작 |
| UI 구조 | 실제 앱과 동일한 하단 탭(가계부/통계/설정) + 상단 DEV 배너 |

### 파일 구조

```
frontend/src/views/MockupView.vue     # 목업 뷰 — 실제 앱과 동일한 UI, mock 데이터로 동작
frontend/src/mocks/                   # mock 데이터 및 함수 (Sprint별로 파일 추가)
└── installment.mock.ts               # Sprint 3 할부 mock (Sprint 5에서 실제 API로 교체)
```

### 개발 흐름

```
1. 요구사항 확정
      ↓
2. MockupView에 해당 Sprint 섹션 추가
   (mock 데이터로 UI 구현, 백엔드 미접근)
      ↓
3. 로컬 /mock-up 접속 → UI/UX 확인 및 피드백
      ↓
4. [확인 완료] 실서비스 이관
   - 프론트엔드: 실제 View/Component에 반영 + 실제 API 연동
   - 백엔드: DB 스키마 변경, API 구현
      ↓
5. MockupView에서 해당 섹션 제거 또는 Sprint 태그 유지
```

### 목업 페이지 기술 구현

```typescript
// frontend/src/router/index.ts
// DEV 환경에서만 라우트 등록 — production 빌드 시 dead code elimination으로 번들 미포함
if (import.meta.env.DEV) {
  routes.push({
    path: '/mock-up',
    name: 'mockup',
    component: () => import('@/views/MockupView.vue'),
  })
}
```

```typescript
// frontend/src/App.vue
// /mock-up 경로에서는 실서비스 하단 탭 숨김 (MockupView 내부 탭 사용)
<nav v-if="route.path !== '/mock-up'" ...>
```

### Sprint 계획 시 준수 사항

- 신규 기능이 포함된 Sprint는 반드시 **목업 구현 → 목업 확인 → 실서비스 구현** 순서로 태스크를 구성합니다.
- 목업 확인 전 실서비스 구현 태스크를 시작하지 않습니다.
- mock 데이터 파일(`frontend/src/mocks/`)에는 해당 Sprint 번호와 실제 API 교체 예정 Sprint를 주석으로 명시합니다.

  ```typescript
  // Sprint N 목업용 — Sprint M에서 실제 API로 교체
  ```

- **코드 수정 전 영향 범위 파악 필수**: 변경하는 코드가 영향을 미치는 모든 케이스(프론트엔드·백엔드 구분 없이)를 먼저 나열하고, 각 케이스가 수정 후에도 올바르게 동작하는지 확인합니다.
  - 예: 신규 등록 / 편집 모드 / 권한별 분기 / API 호출 경로 등
  - 한 케이스를 고치면서 다른 케이스가 깨지지 않도록 합니다.
- **요청하지 않은 기능을 추가하지 않습니다.** 요청의 의도를 임의로 확장하거나, 관련 없는 동작을 함께 변경하지 않습니다. 불명확하면 구현 전에 먼저 확인합니다.
- 검증 원칙 상세: `docs/dev-process.md` 섹션 5 참조
- 배포 후 수동 작업: `deploy.md` 참조 (완료 기록은 `docs/deploy-history/` 아카이브)
- 체크리스트 작성 형식:
  - 완료 항목: `- ✅ 항목 내용`
  - 미완료 항목: `- ⬜ 항목 내용`
  - GFM `[x]`/`[ ]` 대신 이모지를 사용하여 마크다운 미리보기에서 시각적 구분을 보장합니다.

## Git 커밋 및 Push 규칙

- **커밋은 작업 완료 시점에만** 수행합니다. 중간 변경사항은 커밋하지 않습니다.
- **관련 변경사항은 하나의 커밋으로 묶습니다.** fix + docs, feat + style 등 같은 작업 단위는 분리하지 않습니다.
- **명시적 요청 전까지 `git push` 금지.** "push해줘" / "배포해줘" 요청이 있을 때만 push합니다.

## 프론트엔드 UI 규칙

### 레이아웃 패턴 (모바일 앱 구조)

모든 뷰는 `h-full flex flex-col` 구조를 따릅니다:

- 헤더: `flex-shrink-0` (고정)
- 콘텐츠: `flex-1 overflow-y-auto` (스크롤)
- `App.vue`: `h-screen flex flex-col overflow-hidden`, `<main>`은 `flex-1 overflow-hidden`

### 알림 방식

- **브라우저 `alert()` / `confirm()` 사용 금지** → `useDialog` composable 사용
- **성공 알림 없음** — "저장되었습니다" 등의 알림은 추가하지 않습니다. UI 갱신으로 충분합니다.
- **오류 알림**: `showAlert()` (확인 버튼만)
- **삭제 확인**: `showConfirm()` (확인/취소, 확인은 red)

## 프론트엔드 환경변수 관리

프론트엔드는 Vite의 `.env` 파일 시스템으로 환경별 설정을 분리합니다.
**절대로 환경변수를 코드에 하드코딩하지 마세요.** 항상 아래 파일을 수정하세요.

### 환경별 파일

| 파일                        | 적용 환경                        | 빌드 명령                   |
| --------------------------- | -------------------------------- | --------------------------- |
| `frontend/.env.development` | 로컬 개발                        | `vite dev` (자동)           |
| `frontend/.env.staging`     | develop 브랜치 → Render 스테이징 | `vite build --mode staging` |
| `frontend/.env.production`  | main 브랜치 → Render 프로덕션    | `vite build` (자동)         |

### 현재 환경변수 목록

| 키             | 설명                | development             | staging                                        | production                                     |
| -------------- | ------------------- | ----------------------- | ---------------------------------------------- | ---------------------------------------------- |
| `VITE_API_URL` | 백엔드 API 기본 URL | `http://localhost:5244` | `https://budget-tracker-api-51n7.onrender.com` | `https://budget-tracker-api-51n7.onrender.com` |

### 규칙

- `*.local` 파일은 `.gitignore`에 포함 → 커밋 금지 (민감 정보용)
- `.env.development`, `.env.staging`, `.env.production`은 커밋 대상 (URL 등 비민감 정보만 포함)
- 새 환경변수 추가 시 세 파일 모두 업데이트하고 이 표도 함께 갱신
- `render.yaml`의 프론트엔드 `buildCommand`에 `--mode staging` 포함 확인

### Render 배포 시 주의

- `VITE_API_URL`은 **빌드 타임**에 주입됨 → Render 대시보드 환경변수 설정 불필요
- `.env.staging` / `.env.production` 파일 값이 직접 빌드에 반영됨

## 백엔드 환경변수 관리

백엔드는 .NET의 `appsettings.{Environment}.json` 파일 시스템으로 환경을 분리합니다.
**민감 정보(DB 연결 문자열 등)는 절대 git에 커밋하지 마세요.**

### 환경별 파일

| 파일                           | 적용 환경           | git 커밋      | 용도                      |
| ------------------------------ | ------------------- | ------------- | ------------------------- |
| `appsettings.json`             | 전 환경 공통 기본값 | ✅ 커밋       | 로깅 기본값, AllowedHosts |
| `appsettings.Development.json` | 로컬 개발           | ❌ gitignored | DB 연결 문자열, 상세 로깅 |

> `.gitignore` 규칙: `appsettings.*.json` 전부 제외 (`appsettings.json` 제외)

### 환경별 설정값

| 항목                                   | 로컬 (Development)             | Render 배포 (Production)            | 관리 방식             |
| -------------------------------------- | ------------------------------ | ----------------------------------- | --------------------- |
| `ConnectionStrings__DefaultConnection` | `appsettings.Development.json` | **Render 대시보드 env var**         | 민감 → 파일 커밋 불가 |
| `ASPNETCORE_ENVIRONMENT`               | `Development` (자동)           | `Production` (`render.yaml`에 명시) | 비민감                |
| 로깅 레벨                              | Debug (Development.json)       | Information (`appsettings.json`)    | 이미 분리됨           |
| CORS                                   | AllowAnyOrigin (MVP)           | 동일                                | 코드에서 관리         |

### 로컬 개발 초기 설정

`backend/BudgetTracker.Api/appsettings.Development.json` 파일을 직접 생성 (git에 없음):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=<supabase-host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Render 배포 시 필수 env var

`budget-tracker-api` 서비스에 반드시 설정:

- `ConnectionStrings__DefaultConnection` = Supabase 연결 문자열
- `ASPNETCORE_ENVIRONMENT` = `Production` (render.yaml에 이미 포함)

## Notion 기술 문서 관리

- **Notion 루트 페이지**: (새 프로젝트 시작 시 설정 필요)
- **업데이트 원칙**: 사용자가 지시할 때 프로젝트 진행 상황에 맞춰 Notion 문서를 업데이트합니다.
- **업데이트 트리거**: `docs/dev-process.md` 섹션 8.5 참조

## 프로젝트 컨텍스트

- 현재 진행 Sprint: docs/sprint/ 폴더의 최신 파일 참조
- 기술 스택 및 규칙: docs/SPEC.md 참조
- 새 세션 시작 시 반드시 해당 문서를 먼저 읽을 것
