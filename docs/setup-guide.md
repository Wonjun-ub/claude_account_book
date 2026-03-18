# 환경 설정 가이드

> 프로젝트 최초 시작 시 1회 수행하는 환경 설정 가이드입니다.

---

## 1. 사전 요구사항

- [ ] Git
- [ ] ~~[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*
- [ ] [Node.js 16.x](https://nodejs.org) (`frontend/package.json` engines.node 고정값)
- [ ] ~~Supabase 프로젝트 (DB 연결 정보)~~ *(⚠️ Deprecated — v2.0 이후 IndexedDB 사용)*

---

## 2. 저장소 클론

```bash
git clone https://github.com/Wonjun-ub/claude_account_book.git
cd claude_account_book
```

---

## 3. 환경변수 설정

> 전체 환경변수 목록 및 배포 환경별 파일 구조는 `CLAUDE.md` 참조.

### ~~백엔드~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*

~~`backend/BudgetTracker.Api/appsettings.Development.json` 파일을 직접 생성합니다 (git에 없음):~~

```jsonc
// ⚠️ Deprecated (v2.0 이후 불필요)
// {
//   "Logging": { ... },
//   "ConnectionStrings": {
//     "DefaultConnection": "Host=<supabase-host>;..."
//   }
// }
```

### 프론트엔드

`frontend/.env.development` 파일은 저장소에 포함되어 있습니다. v2.0 Local-first 전환으로 `VITE_API_URL`은 미사용 상태입니다. 환경변수 파일은 참조용으로 유지됩니다.

---

## 4. 로컬 개발 환경 실행

### ~~백엔드~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*

```bash
# ⚠️ Deprecated (v2.0 이후 불필요)
# cd backend/BudgetTracker.Api
# dotnet ef database update   # 최초 1회: DB 마이그레이션 적용
# dotnet run                  # → http://localhost:5244/swagger
```

### 프론트엔드

```bash
cd frontend

npm install
npm run dev
# → http://localhost:5173
```

---

## 5. 외부 서비스 설정

### ~~5.1 Supabase~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 불필요)*

~~1. [supabase.com](https://supabase.com)에서 프로젝트 생성 (또는 기존 프로젝트 접속)~~
~~2. Project Settings → Database → Connection string (URI 탭) 복사~~
~~3. 위 `appsettings.Development.json`의 `DefaultConnection`에 입력~~

> v2.0 이후 데이터는 브라우저 IndexedDB(Dexie.js)에 로컬 저장됩니다. 별도 외부 서비스 설정 불필요.

---

## 6. IDE 설정

### VS Code (권장 익스텐션)

| 익스텐션 | ID | 용도 |
|----------|-----|------|
| ~~C# Dev Kit~~ | ~~`ms-dotnettools.csdevkit`~~ | ~~C#/ASP.NET 개발~~ *(⚠️ Deprecated)* |
| Volar | `Vue.volar` | Vue3 지원 |
| Tailwind CSS IntelliSense | `bradlc.vscode-tailwindcss` | Tailwind 자동완성 |
| ESLint | `dbaeumer.vscode-eslint` | TypeScript 린트 |

### ~~JetBrains Rider~~ *(⚠️ Deprecated — v2.0 이후 백엔드 제거로 불필요)*

~~- .NET 지원 내장~~
~~- Vue.js 플러그인 별도 설치 필요~~

---

## 7. Claude Code 설정

이 프로젝트는 Claude Code와 함께 사용하도록 설계되었습니다.

### 에이전트

| 에이전트 | 용도 |
|----------|------|
| `sprint-planner` | 스프린트 계획 수립 |
| `sprint-close` | 스프린트 마무리 (PR, 코드 리뷰, 검증) |
| `hotfix-close` | 핫픽스 마무리 |
| `deploy-prod` | 프로덕션 배포 |

### 스킬

| 스킬 | 용도 |
|------|------|
| `karpathy-guidelines` | 코딩 전 사고 원칙 — 과도한 코드 작성 방지 |
| `writing-plans` | 구현 계획 문서 작성 양식 |

> 스킬 상세: `.claude/skills/` 폴더 참조
