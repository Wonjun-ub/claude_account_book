> **개발 프로세스/검증 절차**: [`docs/dev-process.md`](dev-process.md) 참조
> **롤백 시나리오 상세(DB 백업 포함)**: [`docs/dev-process.md` 섹션 6.4](dev-process.md#64-롤백-시나리오) 참조

## Git 브랜치 전략 & 배포 흐름

### 브랜치 구조

| 브랜치 | 역할 | 배포 환경 |
|--------|------|----------|
| `sprint{n}` | 스프린트 단위 개발 작업 | 로컬 |
| `develop` | 스테이징 통합 브랜치 | 로컬 직접 실행 |
| `main` | 프로덕션 브랜치 | 프로덕션 서버 |
| `hotfix/*` | 긴급 운영 패치 | main + develop 동시 반영 |

---

### 배포 흐름

```
sprint{n}
  ↓ PR & merge (스프린트 완료 시)
develop ──────────────→ 로컬 직접 실행(~~dotnet run +~~ npm run dev)으로 스테이징 검증
  ↓ PR & merge (QA 통과 후)
main    ──────────────→ GitHub Actions → 프로덕션 서버 자동 배포
  ↓ tag
v1.0.0, v1.1.0 ...
```

### Hotfix 배포 흐름

```
hotfix/*
  ↓ PR & merge (긴급 패치)
main    ──────────────→ GitHub Actions → 프로덕션 서버 자동 배포
  ↓ 역머지
develop ──────────────→ main 변경사항 동기화
```

---

### 핵심 규칙

- `main` 직접 push 금지 — 반드시 PR + 리뷰 후 merge
- `develop` → `main` merge는 QA 통과 후 진행
- 긴급 패치는 **`main` 기반**으로 `hotfix/*` 브랜치를 생성하여 작업
- hotfix PR은 **`main`으로 직접** 생성 (develop 거치지 않음)
- main merge 후 반드시 `develop`에 역머지하여 동기화
- hotfix 범위 제한: 파일 3개 이하, 코드 50줄 이하, DB 변경 없음, 새 의존성 없음
- 스프린트 병렬 진행 시 `develop` merge 충돌 주의

---

## CI 파이프라인 (PR 체크)

PR이 `develop` 또는 `main`으로 올라오면 GitHub Actions가 자동으로 실행됩니다.

### 필수 통과 조건

1. ~~**`dotnet build` 성공** — 백엔드 빌드 에러 없음~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*
2. ~~**`dotnet test` 통과** — 백엔드 단위 테스트 전체 통과~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*
3. **`npm run build` 성공** — 프론트엔드 빌드 에러 없음

PR merge는 위 조건이 모두 통과된 후에만 가능합니다 (Branch Protection Rule).

---

## CD 파이프라인 (배포 흐름)

### develop merge 후 (스테이징 검증)

`develop` 브랜치는 로컬에서 직접 실행하여 스테이징 검증합니다.

```bash
# ⚠️ Deprecated (v2.0 이후 백엔드 제거)
# 백엔드
# cd backend/BudgetTracker.Api && dotnet run

# 프론트엔드 (별도 터미널)
cd frontend && npm run dev
```

### main merge 후 (프로덕션 배포)

`main` 브랜치에 merge되면 **Render**가 자동으로 감지하여 배포합니다:

1. ~~백엔드: Render가 `backend/BudgetTracker.Api/` 빌드 후 배포~~ *(⚠️ Deprecated — v2.0 Local-first 전환으로 백엔드 제거)*
2. 프론트엔드: Render가 `frontend/` 빌드 (`vite build`) 후 정적 파일 배포
3. 배포 설정: `render.yaml` 참조

---

## 환경별 설정 관리

| 환경 | 설정 방법 | 비고 |
|------|----------|------|
| 로컬 개발 | ~~`appsettings.Development.json`,~~ `frontend/.env.development` | *(백엔드 설정은 v2.0 이후 불필요)* |
| 프로덕션 | Render 대시보드 환경변수 | CLAUDE.md 참조 |

> 전체 환경변수 목록 및 파일별 설명은 `CLAUDE.md`의 "프론트엔드 환경변수 관리" 섹션 참조.

### Render 프로덕션 필수 환경변수

> ⚠️ Deprecated — v2.0 Local-first 전환 이후 아래 백엔드 환경변수는 불필요합니다.

| 환경변수 | 서비스 | 설명 |
|----------|--------|------|
| ~~`ConnectionStrings__DefaultConnection`~~ | ~~백엔드~~ | ~~Supabase 연결 문자열~~ |
| ~~`ASPNETCORE_ENVIRONMENT`~~ | ~~백엔드~~ | ~~`Production` (render.yaml에 포함)~~ |

---

## 롤백 절차

> 아래는 CI/CD 관점의 롤백 요약입니다.
> 시나리오별 상세 절차(DB 백업 포함)는 [docs/dev-process.md 섹션 6.4](dev-process.md#64-롤백-시나리오) 참조.

### 빠른 롤백 (Render 이전 배포로 복구)

Render 대시보드 → 해당 서비스 → "Deploys" 탭 → 이전 성공 배포 선택 → "Rollback to this deploy"

### ~~DB 마이그레이션 롤백~~ *(⚠️ Deprecated — v2.0 이후 DB 없음)*

```bash
# ⚠️ Deprecated (v2.0 Local-first 전환 이후 EF Core 마이그레이션 불필요)
# EF Core 이전 마이그레이션으로 다운그레이드
# cd backend/BudgetTracker.Api
# dotnet ef migrations list
# dotnet ef database update <이전_마이그레이션_이름>
```

---

## HTTPS/TLS

### 방법 1: Let's Encrypt + certbot (권장)

```bash
# 서버 인스턴스에서
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
```

Nginx 설정에서 certbot이 자동으로 SSL 블록을 추가합니다. 90일마다 자동 갱신됩니다.

### 방법 2: 로드밸런서 SSL

AWS Lightsail 또는 다른 클라우드 콘솔에서 로드밸런서 생성 후 SSL 인증서를 연결합니다.
추가 비용이 발생하지만 관리가 단순합니다.
