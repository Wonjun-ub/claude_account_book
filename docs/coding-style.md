# BudgetTracker 코딩 스타일 가이드

프론트엔드(Vue3/TypeScript)와 백엔드(.NET 8 C#) 모두에 적용되는 가독성 규칙입니다.

---

## 핵심 원칙: 논리 단계별 문단 구분

함수/메서드 내부를 **논리적 단계**에 따라 빈 줄로 나눕니다.
각 단계는 하나의 "문단"처럼 읽혀야 합니다.

### 기본 흐름

```
1. 초기화 / 상태 리셋
2. (빈 줄)
3. 유효성 검사
4. (빈 줄)
5. 비즈니스 로직 / API 호출
6. (빈 줄)
7. 결과 처리 / 반환
```

---

## TypeScript / Vue3

### ✅ 올바른 예

```typescript
async function save() {
  error.value = ''

  const amount = rawAmount()

  if (!amount || amount <= 0) { error.value = '금액을 입력해주세요.'; return }
  if (!form.value.categoryId) { error.value = '카테고리를 선택해주세요.'; return }

  saving.value = true

  try {
    const payload = {
      amount,
      date: form.value.date,
      categoryId: form.value.categoryId,
    }

    await transactionsApi.create(payload)

    emit('saved')
  } catch (e: unknown) {
    error.value = '저장 중 오류가 발생했습니다.'
  } finally {
    saving.value = false
  }
}
```

```typescript
async function loadData() {
  loading.value = true
  selectedCategory.value = null

  try {
    const [txList, sum] = await Promise.all([
      transactionsApi.getAll(params),
      summaryApi.getMonthly(year, month),
    ])

    transactions.value = txList
    summary.value = sum
  } finally {
    loading.value = false
  }
}
```

```typescript
async function addCategory() {
  catError.value = ''

  if (!newCatName.value.trim()) { catError.value = '이름을 입력하세요.'; return }

  try {
    await categoriesApi.create({ name: newCatName.value.trim(), type: newCatType.value })

    newCatName.value = ''
    await store.loadMasterData()
  } catch {
    catError.value = '저장 중 오류가 발생했습니다.'
  }
}
```

### ❌ 잘못된 예

```typescript
async function save() {
  error.value = ''
  const amount = rawAmount()
  if (!amount || amount <= 0) { error.value = '금액을 입력해주세요.'; return }
  saving.value = true
  try {
    const payload = { amount, date: form.value.date }
    await transactionsApi.create(payload)
    emit('saved')
  } finally {
    saving.value = false
  }
}
```

---

## C# / .NET

### ✅ 올바른 예

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
{
    // 유효성 검사
    if (request.Amount <= 0)
        return BadRequest(new { message = "금액은 0보다 커야 합니다." });

    var paymentMethod = await _db.PaymentMethods.FindAsync(request.PaymentMethodId);
    if (paymentMethod is null)
        return BadRequest(new { message = "존재하지 않는 결제수단입니다." });

    // 포인트 잔액 검사
    if (paymentMethod.Type == PaymentMethodType.Point)
    {
        var budget = await _db.PointBudgets.FindAsync(paymentMethod.PointBudgetId);
        if (budget!.RemainingAmount < request.Amount)
            return BadRequest(new { message = "포인트 잔액이 부족합니다." });

        budget.RemainingAmount -= request.Amount;
    }

    // 거래 생성
    var transaction = new Transaction
    {
        Amount = request.Amount,
        Date = request.Date,
        Type = request.Type,
    };

    _db.Transactions.Add(transaction);
    await _db.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, response);
}
```

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    var category = await _db.Categories.FindAsync(id);
    if (category is null)
        return NotFound(new { message = "카테고리를 찾을 수 없습니다." });

    if (category.IsDefault)
        return BadRequest(new { message = "기본 카테고리는 삭제할 수 없습니다." });

    bool hasTransactions = await _db.Transactions.AnyAsync(t => t.CategoryId == id);
    if (hasTransactions)
        return BadRequest(new { message = "연결된 거래가 있어 삭제할 수 없습니다." });

    _db.Categories.Remove(category);
    await _db.SaveChangesAsync();

    return NoContent();
}
```

### ❌ 잘못된 예

```csharp
public async Task<IActionResult> Delete(int id)
{
    var category = await _db.Categories.FindAsync(id);
    if (category is null) return NotFound();
    if (category.IsDefault) return BadRequest();
    bool hasTransactions = await _db.Transactions.AnyAsync(t => t.CategoryId == id);
    if (hasTransactions) return BadRequest();
    _db.Categories.Remove(category);
    await _db.SaveChangesAsync();
    return NoContent();
}
```

---

## 세부 규칙

### 1. try 블록 내부도 단계 구분

```typescript
try {
  // 1단계: API 호출
  await categoriesApi.create(payload)

  // 2단계: 상태 초기화 및 갱신
  newCatName.value = ''
  await store.loadMasterData()
} catch {
  catError.value = '저장 중 오류가 발생했습니다.'
}
```

### 2. computed / 반복문 내부

```typescript
const categoryChips = computed(() => {
  const seen = new Set<number>()
  const result: { id: number; name: string; type: string }[] = []

  for (const tx of transactions.value) {
    if (!seen.has(tx.categoryId)) {
      seen.add(tx.categoryId)
      result.push({ id: tx.categoryId, name: getCategoryName(tx.categoryId, tx.categoryName), type: tx.type })
    }
  }

  return result
})
```

### 3. Promise 상태 변수는 로직 전에 분리

```typescript
// ✅ saving/loading 플래그는 try 전에 별도 문단으로
saving.value = true

try { ... }
```

### 4. 반환문(return) 전 빈 줄

```typescript
// ✅ return 앞에 빈 줄
  return Array.from(map.entries()).sort(...)
}
```

```csharp
// ✅ return 앞에 빈 줄
  return Ok(result);
}
```

### 5. 예외: 한 줄짜리 단순 함수는 문단 구분 불필요

```typescript
// 단순 변환 함수는 빈 줄 없이 작성
function formatAmount(amount: number, type: string) {
  const sign = type === 'Income' ? '+' : '-'
  return `${sign}${amount.toLocaleString()}원`
}
```

---

## Vue 템플릿 섹션 구분

주석으로 섹션을 명확히 표시합니다.

```html
<!-- ── 고정 헤더 ── -->
<div class="flex-shrink-0">...</div>

<!-- ── 스크롤 콘텐츠 ── -->
<div class="flex-1 overflow-y-auto">...</div>

<!-- ── FAB 버튼 ── -->
<button class="fixed ...">...</button>
```

---

## C# 컨트롤러 주석 규칙

각 액션 메서드 위에 HTTP 메서드와 경로를 주석으로 표시합니다.

```csharp
// GET /api/categories?type=Expense
[HttpGet]
public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll(...)

// POST /api/categories
[HttpPost]
public async Task<ActionResult<CategoryResponse>> Create(...)

// DELETE /api/categories/{id}
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
```

---

## 데이터 무결성 패턴

### 1. 원자성 — 복합 생성은 DB 트랜잭션으로 묶기

여러 테이블에 데이터를 동시에 생성하는 경우, 부분 성공(Partial Success)을 막기 위해 **반드시 하나의 DB 트랜잭션**으로 처리합니다.

**❌ 잘못된 예 — SaveChanges를 N번 호출 (부분 성공 위험)**

```csharp
// 원부 저장 (1번 SaveChanges)
var created = await _repo.CreateAsync(master);

// 회차별 거래 각각 SaveChanges — 3번째에서 실패하면 원부만 남음
for (int seq = 1; seq <= totalInstallments; seq++)
{
    await _transactionRepo.CreateAsync(transaction); // SaveChanges 호출
}
```

**✅ 올바른 예 — BatchCreateAsync로 원자적 처리**

```csharp
// Repository 내부에서 EF Core DB 트랜잭션으로 묶음
public async Task<InstallmentTransaction> BatchCreateAsync(
    InstallmentTransaction master,
    IEnumerable<Transaction> transactionDrafts)
{
    await using var dbTx = await _db.Database.BeginTransactionAsync();
    try
    {
        _db.InstallmentTransactions.Add(master);
        await _db.SaveChangesAsync(); // master.Id 확보

        foreach (var draft in transactionDrafts)
        {
            draft.InstallmentTransactionId = master.Id;
            _db.Transactions.Add(draft);
        }

        await _db.SaveChangesAsync(); // 모든 거래 일괄 커밋
        await dbTx.CommitAsync();

        return master;
    }
    catch
    {
        await dbTx.RollbackAsync();
        throw;
    }
}
```

> **규칙**: 원부(Master) + N건 자식 생성은 Repository에 `BatchCreateAsync` 메서드를 두고, 그 안에서 `BeginTransactionAsync → SaveChanges(master) → SaveChanges(children) → Commit` 패턴을 사용합니다.

---

### 2. 멱등성 — 반복 거래 중복 생성 방지

반복 거래 자동 생성(`ApplyRecurringTransactionsAsync`)은 같은 월에 여러 번 호출되어도 거래가 중복 생성되면 안 됩니다.

**구현 원칙**:
- 생성 전에 `GetByRecurringAndDateAsync(recurringId, transactionDate)` 로 해당 날짜에 이미 생성된 거래 존재 여부를 확인합니다.
- 이미 거래가 있으면 skip합니다.

```csharp
// ✅ 멱등성 체크 패턴
bool alreadyCreated = await _transactionRepo.GetByRecurringAndDateAsync(recurring.Id, transactionDate);
if (alreadyCreated)
    continue; // 이미 생성됨 → 건너뜀

await _transactionRepo.CreateAsync(transaction);
```

> **주의**: 애플리케이션 레벨 멱등성 체크는 동시 요청에 취약합니다. 중요한 경우 DB 유니크 제약(`UNIQUE INDEX`)을 추가하는 것을 권장합니다.

---

### 3. 연결성 유지 — 반복/할부 원부 연결(parent_id) 보존

반복·할부로 생성된 단건 거래를 수정할 때 원부와의 연결(`RecurringTransactionId`, `InstallmentTransactionId`, `InstallmentSequence`)을 **절대 덮어쓰지 않습니다**.

| 수정 범위 | 변경 가능 필드 | 변경 금지 필드 |
|-----------|---------------|---------------|
| 단건 수정 | Amount, Date, Memo, CategoryId, PaymentMethodId, IsIncludedInTotal | RecurringTransactionId, InstallmentTransactionId, InstallmentSequence |
| 전체 수정 | 원부 삭제 후 재생성 | — |

```csharp
// ✅ UpdateAsync: parent_id 필드는 업데이트하지 않음
transaction.Amount = request.Amount;
transaction.Date = request.Date.ToUniversalTime();
transaction.CategoryId = request.CategoryId;
// RecurringTransactionId, InstallmentTransactionId, InstallmentSequence ← 건드리지 않음
```

---

### 4. 포인트 잔액 복구 — 검증 먼저, 수정 나중

포인트 결제수단이 변경될 때 이전 잔액 복구 → 새 잔액 차감 순서를 지키되, **엔티티를 수정하기 전에 먼저 잔액 충분 여부를 검증**합니다. 조기 반환(early return) 시 EF Core change tracker에 dirty state가 남는 것을 방지하기 위함입니다.

**❌ 잘못된 예 — 복구 후 검증 (early return 시 dirty state 발생)**

```csharp
oldPaymentMethod.PointBudget.RemainingAmount += transaction.Amount; // 복구 먼저

if (newPaymentMethod.PointBudget.RemainingAmount < request.Amount)
    return (null, "포인트 잔액이 부족합니다."); // early return → dirty state!

newPaymentMethod.PointBudget.RemainingAmount -= request.Amount;
```

**✅ 올바른 예 — 검증 먼저, 복구·차감 나중**

```csharp
// 1단계: 잔액 검증 (엔티티 미수정)
if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
{
    decimal availableBalance = newPaymentMethod.PointBudget.RemainingAmount;
    if (transaction.PaymentMethodId == request.PaymentMethodId
        && oldPaymentMethod.Type == PaymentMethodType.Point
        && oldPaymentMethod.PointBudget is not null)
    {
        availableBalance += transaction.Amount; // 복구 예정 금액 포함
    }
    if (availableBalance < request.Amount)
        return (null, "포인트 잔액이 부족합니다.");
}

// 2단계: 검증 통과 후 안전하게 복구·차감
if (oldPaymentMethod.Type == PaymentMethodType.Point && oldPaymentMethod.PointBudget is not null)
    oldPaymentMethod.PointBudget.RemainingAmount += transaction.Amount;

if (newPaymentMethod.Type == PaymentMethodType.Point && newPaymentMethod.PointBudget is not null)
    newPaymentMethod.PointBudget.RemainingAmount -= request.Amount;
```

---

## 타입 안전성

### 5. TypeScript — mode 파라미터는 Literal Type 사용

API 호출 시 `string` 대신 허용된 값만 컴파일 타임에 검사하는 **Literal Type**을 사용합니다.

**❌ 잘못된 예**

```typescript
deleteWithMode: (id: number, mode: string, ...) => { ... }
// 호출측: deleteWithMode(id, 'alll') — 오타를 컴파일러가 잡지 못함
```

**✅ 올바른 예**

```typescript
// api/index.ts — 허용 값을 명시적으로 선언
export type RecurringDeleteMode = 'all' | 'fromHere' | 'skipMonth'
export type InstallmentDeleteMode = 'all' | 'fromHere' | 'single'

deleteWithMode: (id: number, mode: RecurringDeleteMode, ...) => { ... }
// 호출측: deleteWithMode(id, 'alll') — TS 컴파일 에러 발생
```

### 6. TypeScript — 필터 파라미터 enum 타입 사용

```typescript
// ❌ getAll: (type?: string)
// ✅ getAll: (type?: CategoryType)
categoriesApi.getAll('IncomeX') // ✅ 컴파일 에러로 조기 발견
```

---

## 에러 핸들링

### 7. API 에러 응답 구조

비즈니스 로직 에러는 단순 문자열 대신 `code + message` 구조로 반환합니다. 프론트엔드에서 에러 코드별 분기 처리를 가능하게 합니다.

| 에러 코드 | HTTP 상태 | 설명 |
|-----------|----------|------|
| `POINT_INSUFFICIENT` | 400 | 포인트 잔액 부족 |
| `CATEGORY_NOT_FOUND` | 400 | 존재하지 않는 카테고리 |
| `PAYMENT_METHOD_NOT_FOUND` | 400 | 존재하지 않는 결제수단 |
| `NOT_FOUND` | 404 | 리소스 없음 |
| `INVALID_MODE` | 400 | 유효하지 않은 모드 파라미터 |
| `MISSING_SEQ` | 400 | 회차 번호 파라미터 누락 |
| `MISSING_DATE` | 400 | date 파라미터 누락 |
| `MISSING_YEAR_MONTH` | 400 | year/month 파라미터 누락 |

```csharp
// ✅ 에러 코드를 포함한 구조화된 응답
if (availableBalance < request.Amount)
    return (null, "POINT_INSUFFICIENT");

// Controller에서 코드별 응답 분기
return error switch
{
    "NOT_FOUND"    => NotFound(new { code = error, message = "거래를 찾을 수 없습니다." }),
    "POINT_INSUFFICIENT" => BadRequest(new { code = error, message = $"포인트 잔액이 부족합니다." }),
    null           => NoContent(),
    _              => BadRequest(new { code = error, message = error }),
};
```

---

## 날짜·시간 처리

### 8. DateTime Kind 명시

API 요청으로 받은 `DateTime`의 Kind는 `Unspecified`입니다. `.ToUniversalTime()` 호출 시 서버 로컬 타임존 기준으로 변환되므로, **서버가 UTC 아닌 환경에서 날짜가 밀릴 수 있습니다.**

```csharp
// ❌ Kind=Unspecified 그대로 ToUniversalTime() — 로컬 타임존 영향받음
request.Date.ToUniversalTime()

// ✅ 명시적으로 UTC로 간주 처리
DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
```

> **현재 프로젝트**: Render 배포 환경이 UTC이므로 실질적 영향은 없으나, 타임존에 민감한 로직에서는 `SpecifyKind`를 권장합니다.

### 9. MonthStartDay 엣지 케이스

`DateRangeHelper.GetMonthRange`는 시작일이 전월 말일을 초과하는 경우(`Math.Min`)와 종료일이 당월 말일을 초과하는 경우를 모두 클램핑 처리합니다. 이 로직을 변경하거나 유사 함수를 작성할 때 반드시 다음 케이스를 검증하세요:

- `startDay=31`, 전월=2월(28일) → 시작일=2월 28일
- `startDay=29`, month=3, 전월=2026년 2월 → 시작일=2월 28일 (비윤년)
- `startDay=10`, month=1 → 전년도 12월 10일 ~ 당월 1월 9일

---

## 백엔드 테스트 작성 가이드

### 테스트 프로젝트 구조

```
backend/
├── BudgetTracker.Api/         # 실제 서비스
└── BudgetTracker.Tests/       # 테스트 프로젝트 (xUnit + Moq)
    ├── Helpers/               # DateRangeHelper 등 순수 함수 테스트
    └── Services/              # Service 계층 테스트 (Repository 모킹)
```

- 테스트 파일 위치는 대상 파일 경로를 미러링합니다.
  - `Services/TransactionService.cs` → `BudgetTracker.Tests/Services/TransactionServiceTests.cs`
  - `Helpers/DateRangeHelper.cs` → `BudgetTracker.Tests/Helpers/DateRangeHelperTests.cs`
- 테스트 클래스/파일명은 `{대상클래스}Tests`로 짓습니다.
- `using Xunit;`는 **반드시 명시**합니다 (ImplicitUsings에 포함되지 않음).

### 테스트 유형별 작성 방법

#### 1. 순수 함수 테스트 (Helper/static 메서드)

Repository/DB 없이 입력→출력만 검증합니다.

```csharp
using Xunit;
using BudgetTracker.Api.Helpers;

public class DateRangeHelperTests
{
    [Fact]
    public void GetMonthRange_StartDay25_ReturnsCorrectRange()
    {
        var (from, to) = DateRangeHelper.GetMonthRange(2024, 3, 25);

        Assert.Equal(new DateTime(2024, 2, 25, 0, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2024, 3, 24, 23, 59, 59, DateTimeKind.Utc), to);
    }
}
```

#### 2. Service 계층 테스트 (Repository 모킹)

`Mock<IRepository>` 로 DB를 대체하고 Service 비즈니스 로직만 검증합니다.

```csharp
using Xunit;
using Moq;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Repositories.Interfaces;

public class RecurringServiceTests
{
    private readonly Mock<IRecurringRepository> _recurringRepo = new();
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<ISettingsRepository> _settingsRepo = new();
    private RecurringService CreateService() =>
        new(_recurringRepo.Object, _txRepo.Object, _settingsRepo.Object);

    [Fact]
    public async Task GetPendingAsync_SkippedMonth_ExcludesSkipped()
    {
        // Arrange
        var recurring = new RecurringTransaction { /* ... */ };
        _recurringRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync([recurring]);
        _txRepo.Setup(r => r.HasTransactionInPeriodAsync(/* ... */)).ReturnsAsync(false);

        var svc = CreateService();

        // Act
        var result = await svc.GetPendingAsync(2024, 3);

        // Assert
        Assert.Empty(result);
    }
}
```

### 테스트 추가 기준

새 기능을 구현할 때 다음 중 하나라도 해당하면 테스트를 추가합니다:

| 상황 | 예시 |
|------|------|
| 순수 계산 로직 | 할부 금액 분할(`CalcInstallment`), 날짜 범위 계산 |
| 비즈니스 규칙 분기 | 포인트 잔액 부족 시 거부, 스킵된 반복 제외 |
| 엣지 케이스가 명확한 경우 | 말일 초과 클램핑, 윤년, 단 1개월 할부 |
| 이전에 버그가 있었던 로직 | 잔액 복구 순서 버그 등 |

### 테스트 네이밍 규칙

```
{메서드명}_{시나리오}_{기대결과}
```

```csharp
// ✅ 좋은 예
CalcInstallment_UnevenSplit_RemainderAddedToFirst
GetPendingAsync_SkippedMonth_ExcludesSkipped
GetMonthRange_StartDay31_ClampsToLastDayOfMonth

// ❌ 나쁜 예
Test1
CalcInstallmentTest
TestGetPending
```

### 테스트 실행

```bash
# 전체 솔루션 테스트
dotnet test backend/BudgetTracker.sln

# 테스트 프로젝트만
dotnet test backend/BudgetTracker.Tests

# 상세 출력
dotnet test backend/BudgetTracker.sln --verbosity normal
```

> CI에서는 PR 생성 시 `dotnet test`가 자동으로 실행됩니다 (`.github/workflows/ci.yml`).
