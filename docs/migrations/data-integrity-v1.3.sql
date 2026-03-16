-- =============================================================================
-- BudgetTracker — 데이터 정합성 제약 조건 마이그레이션 (PRD v1.3)
-- 대상 DB : Supabase (PostgreSQL 15)
-- 작성일  : 2026-03-16
-- 적용 방법: Supabase Dashboard > SQL Editor 에서 순서대로 실행
--           또는 EF Core 마이그레이션으로 동일 내용 생성 후 적용
-- =============================================================================
-- ⚠️  주의사항
--   - 운영 DB 적용 전 스테이징에서 먼저 검증
--   - 각 STEP 별로 트랜잭션이 독립적이므로 개별 실행 가능
--   - 백필(BACKFILL) 완료 후 UNIQUE INDEX 생성할 것 (순서 엄수)
-- =============================================================================


-- =============================================================================
-- STEP 1 : 반복 거래 중복 생성 원천 차단 (Idempotency)
--          PRD v1.3 §데이터 정합성 제약 조건 1번
-- =============================================================================

-- 1-A. recurring_target_month 컬럼 추가
--      기존 데이터가 있어도 NULL 허용이므로 즉시 반영 가능
ALTER TABLE "Transactions"
  ADD COLUMN IF NOT EXISTS "RecurringTargetMonth" varchar(7)
    CHECK ("RecurringTargetMonth" IS NULL
        OR "RecurringTargetMonth" ~ '^\d{4}-(0[1-9]|1[0-2])$');
-- CHECK: NULL이거나 'YYYY-MM' 패턴만 허용 (형식 오입력 방지)

COMMENT ON COLUMN "Transactions"."RecurringTargetMonth"
  IS '반복 거래 대상 연월 (YYYY-MM 형식). RecurringTransactionId가 NULL이면 NULL.';


-- 1-B. 기존 반복 거래 백필 (BACKFILL)
--      현재 DB에 반복 거래가 있다면 날짜 기반으로 채워 넣음
--      중복 레코드가 이미 존재한다면 이 UPDATE 완료 후 UNIQUE INDEX 생성 시 오류 발생
--      → 먼저 SELECT로 중복 여부 확인 후 정리

-- 중복 확인 쿼리 (실행 후 결과가 0행이어야 STEP 1-C 진행 가능)
/*
SELECT
    "RecurringTransactionId",
    to_char("Date", 'YYYY-MM') AS target_month,
    COUNT(*) AS cnt
FROM "Transactions"
WHERE "RecurringTransactionId" IS NOT NULL
GROUP BY "RecurringTransactionId", to_char("Date", 'YYYY-MM')
HAVING COUNT(*) > 1;
*/

UPDATE "Transactions"
SET "RecurringTargetMonth" = to_char("Date", 'YYYY-MM')
WHERE "RecurringTransactionId" IS NOT NULL
  AND "RecurringTargetMonth" IS NULL;


-- 1-C. UNIQUE INDEX 생성 (부분 인덱스 — NULL 제외)
--      두 컬럼 모두 NOT NULL인 경우에만 유일성 강제
--      PostgreSQL의 NULL != NULL 규칙상 일반 UNIQUE는 단건 거래에 영향 없지만
--      명시적 부분 인덱스로 의도를 문서화
CREATE UNIQUE INDEX IF NOT EXISTS "uix_recurring_target_month"
  ON "Transactions" ("RecurringTransactionId", "RecurringTargetMonth")
  WHERE "RecurringTransactionId" IS NOT NULL
    AND "RecurringTargetMonth" IS NOT NULL;

COMMENT ON INDEX "uix_recurring_target_month"
  IS '동일 반복 원부 + 동일 연월 중복 생성 방지. Race Condition에서 두 번째 INSERT가 unique_violation(23505)으로 즉시 실패.';


-- =============================================================================
-- STEP 2 : 포인트 예산 마이너스 잔액 방지 (CHECK Constraint)
--          PRD v1.3 §데이터 정합성 제약 조건 2번
-- =============================================================================

-- 2-A. 현재 음수 잔액 존재 여부 사전 확인 (결과가 0행이어야 ADD CONSTRAINT 가능)
/*
SELECT id, "Name", "RemainingAmount"
FROM "PointBudgets"
WHERE "RemainingAmount" < 0;
*/

ALTER TABLE "PointBudgets"
  ADD CONSTRAINT "chk_remaining_amount_non_negative"
    CHECK ("RemainingAmount" >= 0);

COMMENT ON CONSTRAINT "chk_remaining_amount_non_negative" ON "PointBudgets"
  IS '잔액 음수 방지. 동시 결제 시 애플리케이션 레벨 검증을 통과하더라도 DB가 최종 차단. check_violation(23514) 발생 시 거래 실패 처리.';


-- =============================================================================
-- STEP 3 : 날짜 및 타임존 명확화
--          PRD v1.3 §데이터 정합성 제약 조건 3번
-- =============================================================================

-- 3-A. Transactions.CreatedAt → timestamptz 변환
--      EF Core가 생성한 기본 타입이 'timestamp without time zone'인 경우에만 실행
--      이미 timestamptz라면 이 ALTER는 no-op이므로 안전

-- 현재 타입 확인 (실행 후 data_type 컬럼 확인)
/*
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'Transactions'
  AND column_name = 'CreatedAt';
*/

ALTER TABLE "Transactions"
  ALTER COLUMN "CreatedAt" TYPE timestamptz
    USING "CreatedAt" AT TIME ZONE 'UTC';
-- USING: 기존 값을 'UTC 기준으로 해석한 timestamptz'로 변환
--        실제 저장된 값이 이미 UTC라면 의미상 동일

COMMENT ON COLUMN "Transactions"."CreatedAt"
  IS 'UTC 기준 레코드 생성 시각 (timestamptz). 표시 시 KST(+09:00) 변환.';


-- 3-B. Transactions.Date 타입 확인 (date 타입이어야 함)
--      EF Core DateTime → PostgreSQL timestamp로 매핑된 경우 변환 필요
--      이미 date 타입이면 SKIP

-- 현재 타입 확인
/*
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Transactions'
  AND column_name = 'Date';
*/

-- date 타입이 아닌 경우에만 아래 실행:
-- ALTER TABLE "Transactions"
--   ALTER COLUMN "Date" TYPE date
--     USING "Date"::date;

COMMENT ON COLUMN "Transactions"."Date"
  IS '거래 발생 날짜 (date 타입, 순수 날짜). 클라이언트가 로컬 날짜 YYYY-MM-DD 그대로 전송. 서버는 타임존 변환 없이 저장 및 필터링. KST 자정 근처 오프셋 버그 원천 차단.';


-- 3-C. 반복/할부 원부의 날짜 컬럼 동일 원칙 적용
COMMENT ON COLUMN "RecurringTransactions"."StartDate"
  IS '반복 시작 날짜 (date 타입, 타임존 없음).';

COMMENT ON COLUMN "RecurringTransactions"."EndDate"
  IS '반복 종료 날짜 (date 타입, nullable, 타임존 없음). NULL = 무기한.';

COMMENT ON COLUMN "InstallmentTransactions"."StartDate"
  IS '1회차 할부 날짜 (date 타입, 타임존 없음).';


-- =============================================================================
-- 검증 쿼리 — 적용 후 아래를 실행하여 결과 확인
-- =============================================================================

-- V1. 반복 거래 중복 없음 확인 (0행이어야 정상)
/*
SELECT
    "RecurringTransactionId",
    "RecurringTargetMonth",
    COUNT(*) AS cnt
FROM "Transactions"
WHERE "RecurringTransactionId" IS NOT NULL
  AND "RecurringTargetMonth" IS NOT NULL
GROUP BY "RecurringTransactionId", "RecurringTargetMonth"
HAVING COUNT(*) > 1;
*/

-- V2. PointBudgets 음수 잔액 없음 확인 (0행이어야 정상)
/*
SELECT id, "Name", "RemainingAmount"
FROM "PointBudgets"
WHERE "RemainingAmount" < 0;
*/

-- V3. 컬럼 타입 확인
/*
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name IN ('Transactions', 'PointBudgets')
  AND column_name IN ('Date', 'CreatedAt', 'RecurringTargetMonth', 'RemainingAmount')
ORDER BY table_name, column_name;
*/

-- V4. 인덱스/제약 조건 존재 확인
/*
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Transactions'
  AND indexname = 'uix_recurring_target_month';

SELECT conname, consrc
FROM pg_constraint
WHERE conrelid = 'PointBudgets'::regclass
  AND conname = 'chk_remaining_amount_non_negative';
*/
