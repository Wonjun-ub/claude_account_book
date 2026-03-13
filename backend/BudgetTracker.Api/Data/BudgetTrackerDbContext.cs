using BudgetTracker.Api.Models.Entities;
using BudgetTracker.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Data;

public class BudgetTrackerDbContext : DbContext
{
    public BudgetTrackerDbContext(DbContextOptions<BudgetTrackerDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PointBudget> PointBudgets => Set<PointBudget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Enum → 문자열 저장 ──────────────────────────────────────────
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Category>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<PaymentMethod>()
            .Property(p => p.Type)
            .HasConversion<string>();

        modelBuilder.Entity<RecurringTransaction>()
            .Property(r => r.Type)
            .HasConversion<string>();

        // ── Decimal 정밀도 설정 ─────────────────────────────────────────
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PointBudget>()
            .Property(p => p.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PointBudget>()
            .Property(p => p.RemainingAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RecurringTransaction>()
            .Property(r => r.Amount)
            .HasPrecision(18, 2);

        // ── FK 관계 설정 ────────────────────────────────────────────────
        // Transaction → Category: Restrict (카테고리 삭제 시 거래가 있으면 오류)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction → PaymentMethod: Restrict
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.PaymentMethod)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction → RecurringTransaction: SetNull (반복 지출 삭제 시 거래는 유지)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.RecurringTransaction)
            .WithMany(r => r.Transactions)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // RecurringTransaction → Category: Restrict
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne(r => r.Category)
            .WithMany(c => c.RecurringTransactions)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // RecurringTransaction → PaymentMethod: Restrict
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne(r => r.PaymentMethod)
            .WithMany(p => p.RecurringTransactions)
            .HasForeignKey(r => r.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // PaymentMethod → PointBudget: SetNull (포인트 예산 삭제 시 결제수단은 유지)
        modelBuilder.Entity<PaymentMethod>()
            .HasOne(p => p.PointBudget)
            .WithOne(pb => pb.PaymentMethod)
            .HasForeignKey<PaymentMethod>(p => p.PointBudgetId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── 시드 데이터 ─────────────────────────────────────────────────
        // 지출 카테고리 9개
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "식비", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 2, Name = "교통", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 3, Name = "주거", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 4, Name = "통신", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 5, Name = "의료", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 6, Name = "문화/여가", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 7, Name = "교육", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 8, Name = "쇼핑", Type = CategoryType.Expense, IsDefault = true },
            new Category { Id = 9, Name = "기타지출", Type = CategoryType.Expense, IsDefault = true },
            // 수입 카테고리 3개
            new Category { Id = 10, Name = "급여", Type = CategoryType.Income, IsDefault = true },
            new Category { Id = 11, Name = "부수입", Type = CategoryType.Income, IsDefault = true },
            new Category { Id = 12, Name = "기타수입", Type = CategoryType.Income, IsDefault = true }
        );

        // 기본 결제수단 2개
        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Name = "현금", Type = PaymentMethodType.Cash, IsDefault = true },
            new PaymentMethod { Id = 2, Name = "카드", Type = PaymentMethodType.Card, IsDefault = true }
        );

        // 기본 사용자 설정
        modelBuilder.Entity<UserSettings>().HasData(
            new UserSettings { Id = 1, MonthStartDay = 1 }
        );
    }
}
