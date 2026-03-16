using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services;
using Moq;
using Xunit;

namespace BudgetTracker.Tests.Services;

public class InstallmentTransactionServiceTests
{
    // ── DeleteAsync ───────────────────────────────────────────────────────────

    private static InstallmentTransaction MakeMaster(int id = 1) => new()
    {
        Id = id,
        TotalAmount = 300_000m,
        MonthlyAmount = 100_000m,
        FirstMonthAmount = 100_000m,
        TotalInstallments = 3,
        StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CategoryId = 1,
        PaymentMethodId = 1,
        IsActive = true,
        Category = new Category { Id = 1, Name = "식비", Type = CategoryType.Expense },
        PaymentMethod = new PaymentMethod { Id = 1, Name = "현금", Type = PaymentMethodType.Cash },
    };

    private static List<Transaction> MakeTransactions(int masterId, int count) =>
        Enumerable.Range(1, count).Select(seq => new Transaction
        {
            Id = seq,
            Amount = 100_000m,
            InstallmentTransactionId = masterId,
            InstallmentSequence = seq,
            Category = new Category { Id = 1, Name = "식비", Type = CategoryType.Expense },
            PaymentMethod = new PaymentMethod { Id = 1, Name = "현금", Type = PaymentMethodType.Cash },
        }).ToList();

    private static (InstallmentTransactionService svc,
        Mock<IInstallmentTransactionRepository> repoMock,
        Mock<ITransactionRepository> txRepoMock)
        CreateService()
    {
        var repoMock = new Mock<IInstallmentTransactionRepository>();
        var txRepoMock = new Mock<ITransactionRepository>();
        repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        repoMock.Setup(r => r.DeleteTransactionsAsync(It.IsAny<IEnumerable<Transaction>>())).Returns(Task.CompletedTask);
        repoMock.Setup(r => r.DeleteAsync(It.IsAny<InstallmentTransaction>())).Returns(Task.CompletedTask);
        var svc = new InstallmentTransactionService(repoMock.Object, txRepoMock.Object);
        return (svc, repoMock, txRepoMock);
    }

    [Fact]
    public async Task DeleteAsync_ModeAll_DeletesAllTransactionsAndMaster()
    {
        // Arrange
        var (svc, repoMock, _) = CreateService();
        var master = MakeMaster();
        var txs = MakeTransactions(master.Id, 3);

        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(master);
        repoMock.Setup(r => r.GetTransactionsByMasterAsync(1)).ReturnsAsync(txs);

        // Act
        var error = await svc.DeleteAsync(1, "all", null);

        // Assert
        Assert.Null(error);
        repoMock.Verify(r => r.DeleteTransactionsAsync(txs), Times.Once);
        repoMock.Verify(r => r.DeleteAsync(master), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ModeFromHere_DeletesFromSequenceOnward()
    {
        // Arrange
        var (svc, repoMock, _) = CreateService();
        var master = MakeMaster();
        var txsFromSeq2 = MakeTransactions(master.Id, 3).Where(t => t.InstallmentSequence >= 2).ToList();

        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(master);
        repoMock.Setup(r => r.GetTransactionsFromSeqAsync(1, 2)).ReturnsAsync(txsFromSeq2);

        // Act
        var error = await svc.DeleteAsync(1, "fromHere", seq: 2);

        // Assert
        Assert.Null(error);
        repoMock.Verify(r => r.GetTransactionsFromSeqAsync(1, 2), Times.Once);
        repoMock.Verify(r => r.DeleteTransactionsAsync(txsFromSeq2), Times.Once);
        // 원부는 삭제하지 않음
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<InstallmentTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ModeSingle_DeletesOnlySpecifiedSequence()
    {
        // Arrange
        var (svc, repoMock, _) = CreateService();
        var master = MakeMaster();
        var seq2Tx = MakeTransactions(master.Id, 3).First(t => t.InstallmentSequence == 2);

        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(master);
        repoMock.Setup(r => r.GetTransactionBySeqAsync(1, 2)).ReturnsAsync(seq2Tx);

        // Act
        var error = await svc.DeleteAsync(1, "single", seq: 2);

        // Assert
        Assert.Null(error);
        repoMock.Verify(r => r.GetTransactionBySeqAsync(1, 2), Times.Once);
        repoMock.Verify(r => r.DeleteTransactionsAsync(
            It.Is<IEnumerable<Transaction>>(list => list.Single() == seq2Tx)), Times.Once);
        // 원부는 삭제하지 않음
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<InstallmentTransaction>()), Times.Never);
    }


    // ── CalcInstallment ───────────────────────────────────────────────────────
    // floor 방식: 나머지는 1회차에 합산

    [Fact]
    public void CalcInstallment_EvenSplit_ReturnsSameMonthlyAndFirst()
    {
        // 300,000 / 3 = 100,000 나머지 0
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(300_000m, 3);

        Assert.Equal(100_000m, monthly);
        Assert.Equal(100_000m, first);
    }

    [Fact]
    public void CalcInstallment_UnevenSplit_RemainderAddedToFirst()
    {
        // 100,000 / 3 → floor=33333, remainder=1, first=33334
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(100_000m, 3);

        Assert.Equal(33_333m, monthly);
        Assert.Equal(33_334m, first);
        // 합산 검증: first + monthly * (3-1) = 100,000
        Assert.Equal(100_000m, first + monthly * 2);
    }

    [Fact]
    public void CalcInstallment_SingleMonth_ReturnsFullAmountToFirst()
    {
        // 1개월 할부 → 전액이 1회차
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(50_000m, 1);

        Assert.Equal(50_000m, monthly);
        Assert.Equal(50_000m, first);
    }

    [Fact]
    public void CalcInstallment_LargeRemainder_TotalAmountPreserved()
    {
        // 10,000 / 7 → floor=1428, remainder=4, first=1432
        // 합산: 1432 + 1428*6 = 1432 + 8568 = 10000
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(10_000m, 7);

        Assert.Equal(1_428m, monthly);
        Assert.Equal(1_432m, first);
        Assert.Equal(10_000m, first + monthly * 6);
    }

    [Fact]
    public void CalcInstallment_SmallAmount_TotalAmountPreserved()
    {
        // 1 / 2 → floor=0, remainder=1, first=1, monthly=0
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(1m, 2);

        Assert.Equal(0m, monthly);
        Assert.Equal(1m, first);
        Assert.Equal(1m, first + monthly * 1);
    }

    [Fact]
    public void CalcInstallment_DecimalAmount_TotalAmountPreserved()
    {
        // 1000.50 / 3 → floor=333, remainder=1.50, first=334.50
        var (monthly, first) = InstallmentTransactionService.CalcInstallment(1000.50m, 3);

        Assert.Equal(333m, monthly);
        Assert.Equal(334.50m, first);
        Assert.Equal(1000.50m, first + monthly * 2);
    }
}
