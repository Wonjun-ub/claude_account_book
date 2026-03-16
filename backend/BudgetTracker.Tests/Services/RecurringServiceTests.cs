using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services;
using Moq;
using Xunit;

namespace BudgetTracker.Tests.Services;

public class RecurringServiceTests
{
    private readonly Mock<IRecurringRepository> _recurringRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ISettingsRepository> _settingsRepoMock = new();
    private readonly RecurringService _service;

    public RecurringServiceTests()
    {
        _service = new RecurringService(
            _recurringRepoMock.Object,
            _transactionRepoMock.Object,
            _settingsRepoMock.Object);

        // 기본: monthStartDay=1 (일반 달력 월)
        _settingsRepoMock.Setup(s => s.GetAsync())
            .ReturnsAsync(new UserSettings { MonthStartDay = 1 });
    }

    // ── GetPendingAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingAsync_ActiveWithNoSkipAndNoTransaction_ReturnsPending()
    {
        // 스킵 없음, 거래 없음 → pending에 포함
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15);
        SetupActiveList(recurring);
        SetupHasTransaction(1, false);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Single(result);
        Assert.Equal(1, result.First().Id);
    }

    [Fact]
    public async Task GetPendingAsync_SkippedMonth_Excluded()
    {
        // 2026년 3월 스킵 등록 → pending에서 제외
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15,
            skips: [new RecurringSkip { Year = 2026, Month = 3 }]);
        SetupActiveList(recurring);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingAsync_AlreadyHasTransaction_Excluded()
    {
        // 이미 거래가 존재하면 pending에서 제외 (중복 방지)
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15);
        SetupActiveList(recurring);
        SetupHasTransaction(1, true);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingAsync_EndDateBeforePeriod_Excluded()
    {
        // EndDate가 기간 시작일보다 이전이면 제외
        // 기간: 2026-03-01 ~ 2026-03-31, EndDate = 2026-02-28
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15,
            endDate: new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        SetupActiveList(recurring);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingAsync_StartDateAfterPeriod_Excluded()
    {
        // StartDate가 기간 종료일보다 이후이면 제외
        // 기간: 2026-03-01 ~ 2026-03-31, StartDate = 2026-04-01
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15,
            startDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        SetupActiveList(recurring);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingAsync_StartDateWithinPeriod_Included()
    {
        // StartDate가 기간 내에 있으면 포함
        // 기간: 2026-03-01 ~ 2026-03-31, StartDate = 2026-03-10
        var recurring = MakeRecurring(id: 1, dayOfMonth: 15,
            startDate: new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        SetupActiveList(recurring);
        SetupHasTransaction(1, false);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetPendingAsync_MultipleRecurrings_ReturnsOnlyPending()
    {
        // 3개 반복: 1번은 pending, 2번은 스킵됨, 3번은 이미 거래 있음
        var recurring1 = MakeRecurring(id: 1, dayOfMonth: 5);
        var recurring2 = MakeRecurring(id: 2, dayOfMonth: 10,
            skips: [new RecurringSkip { Year = 2026, Month = 3 }]);
        var recurring3 = MakeRecurring(id: 3, dayOfMonth: 20);

        SetupActiveList(recurring1, recurring2, recurring3);
        SetupHasTransaction(1, false);
        SetupHasTransaction(3, true);

        var result = (await _service.GetPendingAsync(2026, 3)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetPendingAsync_CustomMonthStartDay_UsesCorrectPeriod()
    {
        // monthStartDay=25: 3월 기간 = 2026-02-25 ~ 2026-03-24
        _settingsRepoMock.Setup(s => s.GetAsync())
            .ReturnsAsync(new UserSettings { MonthStartDay = 25 });

        // EndDate=2026-03-01: 기간(2026-02-25~2026-03-24) 내에 있으므로 포함
        var recurring = MakeRecurring(id: 1, dayOfMonth: 1,
            endDate: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        SetupActiveList(recurring);
        SetupHasTransaction(1, false);

        var result = await _service.GetPendingAsync(2026, 3);

        Assert.Single(result);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    private static RecurringTransaction MakeRecurring(
        int id,
        int dayOfMonth,
        DateTime? startDate = null,
        DateTime? endDate = null,
        IEnumerable<RecurringSkip>? skips = null) => new()
    {
        Id = id,
        Amount = 10_000m,
        CategoryId = 1,
        PaymentMethodId = 1,
        Type = RecurringType.Fixed,
        DayOfMonth = dayOfMonth,
        IsActive = true,
        StartDate = startDate,
        EndDate = endDate,
        RecurringSkips = skips?.ToList() ?? [],
        Category = new Category { Id = 1, Name = "식비", Type = CategoryType.Expense },
        PaymentMethod = new PaymentMethod { Id = 1, Name = "현금", Type = PaymentMethodType.Cash },
    };

    private void SetupActiveList(params RecurringTransaction[] items)
    {
        _recurringRepoMock.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(items);
    }

    private void SetupHasTransaction(int recurringId, bool hasTransaction)
    {
        _recurringRepoMock.Setup(r =>
                r.HasTransactionInPeriodAsync(recurringId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(hasTransaction);
    }
}
