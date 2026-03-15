# 배포 후 수동 작업 가이드

> **목적**: 현재 완료되지 않은 수동 검증/작업 항목만 유지합니다.
> 완료된 기록은 `docs/deploy-history/YYYY-MM-DD.md`로 이동됩니다.

---

## 현재 미완료 항목

### Sprint 5 — 할부/반복 거래 실서비스 이관 (2026-03-15)

#### 자동 검증

- ✅ `npm test` — 54 케이스 전체 통과
- ✅ `npm run build` — TypeScript 타입 오류 0건, 빌드 성공
- ✅ `dotnet build` — 경고 0개, 오류 0개
- ✅ DB 마이그레이션 Supabase 적용 완료
- ⬜ 백엔드 서버 미실행 — dotnet test, API curl, Playwright 건너뜀

#### 수동 검증 필요 항목

- ⬜ `docker compose up --build` — 로컬 Docker 환경에서 전체 스택 실행 확인
- ⬜ 할부 등록 플로우: 지출 탭 → 할부 토글 ON → 개월수 입력 → 미리보기 확인 → 저장 → 목록 배지 표시
- ⬜ 할부 삭제: X 버튼 → bottom sheet → all/fromHere/single 3가지 모드 각각 동작 확인
- ⬜ 반복 거래 등록: 부가 정보 토글 → dayOfMonth/endDate 입력 → 저장 → 예정 배너 표시 확인
- ⬜ 반복 거래 삭제: all/fromHere/skipMonth 3가지 모드 동작 확인
- ⬜ 할부 금액 계산 검증: 100,000원/3개월 → 1회차 33,334원, 2~3회차 33,333원
- ⬜ MonthlySummary incomeCount/expenseCount 표시 확인
- ⬜ UI 디자인/시각적 품질 확인

---

## 참고

- 검증 원칙: `docs/dev-process.md` 섹션 5
- 배포 이력: `docs/deploy-history/`
- 롤백 방법: `docs/dev-process.md` 섹션 6.4
