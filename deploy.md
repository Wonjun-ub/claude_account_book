# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## 현재 미완료 항목

### Sprint 1 — 백엔드 핵심 API (2026-03-13)

#### 자동 검증

- ⬜ Docker 미실행으로 자동 검증 미수행 (pytest, API curl, Playwright 모두 건너뜀)

#### 수동 검증 필요 항목

- ⬜ `docker compose up --build` — 로컬 Docker 환경에서 전체 스택 실행 확인
- ⬜ EF Core 마이그레이션 Supabase 적용 (`dotnet ef database update`)
- ⬜ Swagger UI 접속 확인 (`http://localhost:5000/swagger`)
- ⬜ 시나리오 1: 카테고리 생성 → 결제수단 생성 → 거래 생성 → 거래 수정 → 거래 삭제
- ⬜ 시나리오 2: 포인트 예산 생성 → 포인트 결제수단으로 거래 생성 → 잔액 차감 확인
- ⬜ 시나리오 3: 반복 지출 등록 → 월별 요약 API 호출 → 거래 자동 생성 확인
- ⬜ 시나리오 4: 커스텀 월 시작일(25일) 설정 → 월별 요약 날짜 범위 정확성 확인
- ⬜ 시나리오 5: 검색 파라미터 조합 필터링 정상 동작 확인
- ⬜ PR 설명에 수동 검증 결과 스크린샷 첨부 (또는 .http 파일 응답 결과)

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
