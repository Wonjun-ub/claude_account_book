# BudgetTracker — 가계부 웹앱

복지포인트·식대 등 **회사 지원 예산**과 개인 지출을 함께 관리하는 개인 가계부 웹앱입니다.
급여일 기준 **커스텀 월 시작일**, **반복 지출 자동화**, **포인트 잔액 실시간 추적** 기능에 집중합니다.

> 상세 기능 명세 및 목표: [`docs/PRD.md`](docs/PRD.md)

## 기술 스택

- **백엔드**: .NET 9 Web API (C#) + Entity Framework Core + Supabase PostgreSQL
- **프론트엔드**: Vue3 + Vite + TypeScript + Tailwind CSS
- **배포**: Render (자동 배포)

## 요구사항

- .NET 9 SDK
- Node.js 20+ (프론트엔드 빌드)
- PostgreSQL (로컬) 또는 Supabase 연결 정보

## 로컬 실행 방법

### 백엔드

```bash
# 1. 환경 설정
cp .env.example backend/BudgetTracker.Api/appsettings.Development.json
# appsettings.Development.json에서 ConnectionStrings.DefaultConnection 수정

# 2. 마이그레이션 적용
cd backend/BudgetTracker.Api
dotnet ef database update

# 3. 서버 실행
dotnet run
# → http://localhost:5000/swagger 에서 Swagger UI 확인
```

### 프론트엔드

```bash
cd frontend
cp .env.example .env
# .env에서 VITE_API_URL 수정 (기본: http://localhost:5000)
npm install
npm run dev
# → http://localhost:5173
```

## API 엔드포인트

| 메서드 | 경로 | 설명 |
|--------|------|------|
| GET/POST | /api/transactions | 거래 목록/생성 (검색/필터 지원) |
| GET/PUT/DELETE | /api/transactions/{id} | 거래 상세/수정/삭제 |
| GET/POST/PUT/DELETE | /api/categories | 카테고리 관리 |
| GET/POST/PUT/DELETE | /api/payment-methods | 결제수단 관리 |
| GET/POST | /api/point-budgets | 포인트 예산 관리 |
| GET/POST/DELETE | /api/recurring-transactions | 반복 지출 관리 |
| GET | /api/summary/monthly | 월별 요약 (커스텀 시작일 기준) |
| GET | /api/summary/category | 카테고리별 집계 |
| GET | /api/summary/trend | 월별 추이 |
| GET/PUT | /api/settings | 사용자 설정 |

## 개발 워크플로우

`docs/dev-process.md` 참조

## 브랜치 전략

- `sprint{n}` → `develop` (PR) → `main` (배포)
- `hotfix/*` → `main` (직접 배포) → `develop` 역머지

자세한 CI/CD 정책: `docs/ci-policy.md`
