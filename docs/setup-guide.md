# 환경 설정 가이드

> 프로젝트 최초 시작 시 1회 수행하는 환경 설정 가이드입니다.

---

## 1. 사전 요구사항

- [ ] Git
- [ ] [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ ] [Node.js 20+](https://nodejs.org)
- [ ] Supabase 프로젝트 (DB 연결 정보)

---

## 2. 저장소 클론

```bash
git clone https://github.com/Wonjun-ub/claude_account_book.git
cd claude_account_book
```

---

## 3. 환경변수 설정

> 전체 환경변수 목록 및 배포 환경별 파일 구조는 `CLAUDE.md` 참조.

### 백엔드

`backend/BudgetTracker.Api/appsettings.Development.json` 파일을 직접 생성합니다 (git에 없음):

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

Supabase 연결 정보: Supabase 대시보드 → Project Settings → Database → Connection string (URI 탭)

### 프론트엔드

`frontend/.env.development` 파일은 저장소에 포함되어 있습니다. 로컬 백엔드 포트가 다를 경우 `.env.development.local`을 생성하여 오버라이드하세요 (git 미추적):

```env
VITE_API_URL=http://localhost:5244
```

---

## 4. 로컬 개발 환경 실행

### 백엔드

```bash
cd backend/BudgetTracker.Api

# 최초 1회: DB 마이그레이션 적용
dotnet ef database update

# 개발 서버 실행
dotnet run
# → http://localhost:5244/swagger 에서 Swagger UI 확인
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

### 5.1 Supabase (필수)

1. [supabase.com](https://supabase.com)에서 프로젝트 생성 (또는 기존 프로젝트 접속)
2. Project Settings → Database → Connection string (URI 탭) 복사
3. 위 `appsettings.Development.json`의 `DefaultConnection`에 입력

---

## 6. IDE 설정

### VS Code (권장 익스텐션)

| 익스텐션 | ID | 용도 |
|----------|-----|------|
| C# Dev Kit | `ms-dotnettools.csdevkit` | C#/ASP.NET 개발 |
| Volar | `Vue.volar` | Vue3 지원 |
| Tailwind CSS IntelliSense | `bradlc.vscode-tailwindcss` | Tailwind 자동완성 |
| ESLint | `dbaeumer.vscode-eslint` | TypeScript 린트 |

### JetBrains Rider

- .NET 지원 내장
- Vue.js 플러그인 별도 설치 필요

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
