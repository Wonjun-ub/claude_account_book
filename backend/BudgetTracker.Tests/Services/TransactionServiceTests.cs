using BudgetTracker.Api.DTOs.Requests;
using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services;
using Moq;
using Xunit;

namespace BudgetTracker.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IPaymentMethodRepository> _paymentMethodRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ISettingsRepository> _settingsRepoMock = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(
            _transactionRepoMock.Object,
            _paymentMethodRepoMock.Object,
            _categoryRepoMock.Object,
            _settingsRepoMock.Object);
    }

    // ── UpdateAsync — 포인트 잔액 복구 패턴 ──────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenExpenseAmountChanges_RecoversThenDeductsCorrectly()
    {
        // Arrange: 포인트 결제수단, 기존 금액 10_000, 새 금액 15_000, 잔액 20_000
        var pointBudget = new PointBudget { Id = 1, TotalAmount = 50_000m, RemainingAmount = 20_000m };
        var pointMethod = new PaymentMethod
        {
            Id = 1, Name = "복지포인트", Type = PaymentMethodType.Point,
            PointBudgetId = 1, PointBudget = pointBudget,
        };
        var category = new Category { Id = 1, Name = "식비", Type = CategoryType.Expense };
        var existingTx = new Transaction
        {
            Id = 1, Amount = 10_000m, Type = TransactionType.Expense,
            CategoryId = 1, Category = category,
            PaymentMethodId = 1, PaymentMethod = pointMethod,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        _transactionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTx);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _paymentMethodRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pointMethod);
        _transactionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

        var request = new UpdateTransactionRequest
        {
            Amount = 15_000m, Type = TransactionType.Expense,
            CategoryId = 1, PaymentMethodId = 1,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        // Act
        var (response, error) = await _service.UpdateAsync(1, request);

        // Assert
        Assert.Null(error);
        Assert.NotNull(response);
        // 잔액: 20_000 복구(+10_000) = 30_000 → 차감(-15_000) = 15_000
        Assert.Equal(15_000m, pointBudget.RemainingAmount);
    }

    [Fact]
    public async Task UpdateAsync_WhenTypeChangesFromExpenseToIncome_RecoversPreviousDeduction()
    {
        // Arrange: 기존 포인트 결제, 새 결제수단은 현금으로 변경
        var pointBudget = new PointBudget { Id = 1, TotalAmount = 50_000m, RemainingAmount = 20_000m };
        var pointMethod = new PaymentMethod
        {
            Id = 1, Name = "복지포인트", Type = PaymentMethodType.Point,
            PointBudgetId = 1, PointBudget = pointBudget,
        };
        var cashMethod = new PaymentMethod
        {
            Id = 2, Name = "현금", Type = PaymentMethodType.Cash,
            PointBudget = null,
        };
        var category = new Category { Id = 1, Name = "급여", Type = CategoryType.Income };
        var existingTx = new Transaction
        {
            Id = 2, Amount = 5_000m, Type = TransactionType.Expense,
            CategoryId = 1, Category = category,
            PaymentMethodId = 1, PaymentMethod = pointMethod,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        _transactionRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existingTx);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _paymentMethodRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(cashMethod);
        _transactionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

        var request = new UpdateTransactionRequest
        {
            Amount = 5_000m, Type = TransactionType.Income,
            CategoryId = 1, PaymentMethodId = 2,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        // Act
        var (response, error) = await _service.UpdateAsync(2, request);

        // Assert
        Assert.Null(error);
        Assert.NotNull(response);
        // 이전 포인트 결제 복구: 20_000 + 5_000 = 25_000
        Assert.Equal(25_000m, pointBudget.RemainingAmount);
    }
}
