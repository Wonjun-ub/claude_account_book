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
