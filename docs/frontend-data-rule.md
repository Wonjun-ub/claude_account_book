# 프론트엔드 데이터 처리 규칙

백엔드 API 호출을 최소화하고 프론트에서 즉각적인 피드백을 주기 위한 규칙입니다.

---

## 핵심 원칙: 프론트 우선 처리 (Frontend-First)

> 사용자가 느끼는 속도 = API 응답 속도가 아니라 화면 업데이트 속도

API 호출이 필요한 경우라도, 결과가 예측 가능하면 **화면을 먼저 업데이트**하고 서버 요청은 백그라운드에서 처리합니다.

---

## 규칙 1: 낙관적 업데이트 (Optimistic Update)

삭제/수정처럼 결과가 거의 항상 성공하는 작업은 **서버 응답을 기다리지 않고 먼저 UI를 업데이트**합니다.

### ✅ 올바른 예 (낙관적 삭제)

```typescript
async function handleDelete(id: number) {
  // 1. 즉시 UI에서 제거
  const tx = transactions.value.find(t => t.id === id)
  transactions.value = transactions.value.filter(t => t.id !== id)

  // 2. 파생 상태도 즉시 업데이트
  if (summary.value && tx?.isIncludedInTotal) {
    summary.value = { ...summary.value, totalExpense: summary.value.totalExpense - tx.amount }
  }

  // 3. 서버 요청은 백그라운드
  await transactionsApi.delete(id)
}
```

### ❌ 잘못된 예 (서버 응답 후 전체 재조회)

```typescript
async function handleDelete(id: number) {
  await transactionsApi.delete(id)
  await loadData()  // 전체 목록을 다시 불러오는 것은 불필요한 API 호출
}
```

---

## 규칙 2: 로컬 필터는 서버 재요청 없이 처리

이미 로드된 데이터에서 필터링/정렬은 `computed`로 클라이언트에서 처리합니다.

```typescript
// ✅ 서버 재요청 없이 computed로 필터 (ID 기준)
const filteredTransactions = computed(() => {
  if (selectedCategory.value === null) return transactions.value
  return transactions.value.filter(tx => tx.categoryId === selectedCategory.value)
})
```

---

## 규칙 3: 마스터 데이터는 앱 초기 1회 로드

카테고리, 결제수단, 설정 등 변경 빈도가 낮은 데이터는 앱 시작 시 1회만 로드하고 Pinia 스토어에 캐싱합니다.

- `store.loadMasterData()` → `App.vue`에서 `onMounted` 1회 호출
- 수정/추가/삭제 후에는 전체 재조회 대신 **로컬 스토어만 업데이트** (가능한 경우)

---

## 규칙 4: API 호출이 필요한 경우

다음 경우에만 서버 재조회를 허용합니다:

| 상황 | 이유 |
|------|------|
| 월 변경 | 다른 월 데이터는 캐시 없음 |
| 검색어 변경 | 서버 측 ILike 필터 필요 |
| 거래 추가/수정 후 목록 갱신 | 서버 생성 ID, 반복 거래 자동 생성 반영 |
| 포인트 잔액 변경 후 결제수단 목록 | 잔액 최신화 필요 |

---

## 규칙 5: Enum 값은 한국어로 표시

백엔드에서 오는 Enum 문자열(Income, Expense, Cash, Card, Point 등)은 화면에 그대로 노출하지 않고 반드시 한국어로 매핑합니다.

```typescript
// ✅ 매핑 객체 사용
const methodTypeLabel: Record<PaymentMethodType, string> = {
  Cash: '현금',
  Card: '카드',
  Point: '포인트',
}

// 템플릿에서
{{ methodTypeLabel[m.type] }}  // "현금", "카드", "포인트"
```
