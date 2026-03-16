using BudgetTracker.Api.Services;
using Xunit;

namespace BudgetTracker.Tests.Services;

public class InstallmentTransactionServiceTests
{
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
