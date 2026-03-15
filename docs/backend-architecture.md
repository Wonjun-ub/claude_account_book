# BudgetTracker 백엔드 아키텍처

## 3계층 구조

```
HTTP 요청
   ↓
Controllers/         → 요청 수신 · 응답 반환만 담당
   ↓  (IXxxService)
Services/            → 비즈니스 로직 (유효성 검사, 포인트 차감 등)
   ↓  (IXxxRepository)
Repositories/        → DbContext 직접 접근 (CRUD 쿼리)
   ↓
Data/BudgetTrackerDbContext
   ↓
Supabase PostgreSQL
```

---

## 폴더 구조

```
BudgetTracker.Api/
├── Controllers/
│   ├── TransactionsController.cs
│   ├── CategoriesController.cs
│   ├── PaymentMethodsController.cs
│   ├── PointBudgetsController.cs
│   ├── RecurringTransactionsController.cs
│   ├── SettingsController.cs
│   └── SummaryController.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── ITransactionService.cs
│   │   ├── ICategoryService.cs
│   │   ├── IPaymentMethodService.cs
│   │   ├── IPointBudgetService.cs
│   │   ├── IRecurringService.cs
│   │   ├── ISettingsService.cs
│   │   └── ISummaryService.cs
│   ├── TransactionService.cs
│   ├── CategoryService.cs
│   ├── PaymentMethodService.cs
│   ├── PointBudgetService.cs
│   ├── RecurringService.cs
│   ├── SettingsService.cs
│   └── SummaryService.cs
│
├── Repositories/
│   ├── Interfaces/
│   │   ├── ITransactionRepository.cs
│   │   ├── ICategoryRepository.cs
│   │   ├── IPaymentMethodRepository.cs
│   │   ├── IPointBudgetRepository.cs
│   │   ├── IRecurringRepository.cs
│   │   └── ISettingsRepository.cs
│   ├── TransactionRepository.cs
│   ├── CategoryRepository.cs
│   ├── PaymentMethodRepository.cs
│   ├── PointBudgetRepository.cs
│   ├── RecurringRepository.cs
│   └── SettingsRepository.cs
│
├── Data/
│   └── BudgetTrackerDbContext.cs
│
├── Models/
│   ├── Entities/
│   └── Enums/
│
├── DTOs/
│   ├── Requests/
│   └── Responses/
│
└── Helpers/
    └── DateRangeHelper.cs
```

---

## 계층별 역할

### Controller
- HTTP 요청 수신 및 응답 반환만 담당
- `IXxxService`를 생성자 주입으로 사용
- 비즈니스 로직 없음
- 서비스 결과에 따라 적절한 HTTP 상태 코드 반환

```csharp
[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    // GET /api/transactions
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ...)
    {
        var result = await _service.GetAllAsync(...);
        return Ok(result);
    }
}
```

### Service
- 비즈니스 로직 처리
- `IXxxRepository`를 생성자 주입으로 사용
- 오류 반환: `(Response?, string? errorCode)` 튜플 패턴
  - `"NOT_FOUND"` → 컨트롤러에서 404 반환
  - `"BAD_REQUEST:메시지"` → 컨트롤러에서 400 반환
- 주요 비즈니스 로직:
  - 포인트 결제 시 잔액 확인 및 차감/복구
  - 반복 지출 자동 거래 생성 (멱등성 보장)
  - 월 시작일 기준 날짜 범위 계산

```csharp
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly IPaymentMethodRepository _paymentRepo;

    public TransactionService(
        ITransactionRepository repo,
        IPaymentMethodRepository paymentRepo)
    {
        _repo = repo;
        _paymentRepo = paymentRepo;
    }

    public async Task<(TransactionResponse?, string?)> CreateAsync(CreateTransactionRequest request)
    {
        var paymentMethod = await _paymentRepo.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod is null)
            return (null, "NOT_FOUND");

        // 포인트 잔액 확인 및 차감
        if (paymentMethod.Type == PaymentMethodType.Point)
        {
            var budget = await _pointBudgetRepo.GetByIdAsync(paymentMethod.PointBudgetId!.Value);
            if (budget!.RemainingAmount < request.Amount)
                return (null, "BAD_REQUEST:포인트 잔액이 부족합니다.");

            budget.RemainingAmount -= request.Amount;
            await _pointBudgetRepo.UpdateAsync(budget);
        }

        var transaction = new Transaction { ... };
        var created = await _repo.CreateAsync(transaction);

        return (MapToResponse(created), null);
    }
}
```

### Repository
- `BudgetTrackerDbContext` 직접 접근
- CRUD 쿼리만 담당, 비즈니스 로직 없음
- EF Core LINQ 쿼리 작성

```csharp
public class TransactionRepository : ITransactionRepository
{
    private readonly BudgetTrackerDbContext _db;

    public TransactionRepository(BudgetTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _db.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMethod)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
```

---

## DI 등록 (Program.cs)

```csharp
// Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPointBudgetRepository, PointBudgetRepository>();
builder.Services.AddScoped<IRecurringRepository, RecurringRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

// Services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<IPointBudgetService, PointBudgetService>();
builder.Services.AddScoped<IRecurringService, RecurringService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
```

---

## 규칙

### 새 기능 추가 시
1. Repository 인터페이스에 메서드 추가 → 구현체에 구현
2. Service 인터페이스에 메서드 추가 → 구현체에서 Repository 호출
3. Controller에서 Service 호출만 추가

### 금지 사항
- Controller에서 `DbContext` 직접 주입 금지
- Controller에서 비즈니스 로직 작성 금지
- Repository에서 비즈니스 로직 작성 금지 (단순 쿼리만)
- Service에서 `DbContext` 직접 주입 금지 (Repository를 통해서만 접근)

### 예외
- `SummaryService`: 복잡한 집계 쿼리(GroupBy 등)는 Repository 분리 대신 DbContext 직접 사용 허용
