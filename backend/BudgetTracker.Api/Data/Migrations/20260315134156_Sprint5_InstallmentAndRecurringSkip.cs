using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5_InstallmentAndRecurringSkip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingInstallments",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "TotalInstallments",
                table: "RecurringTransactions");

            migrationBuilder.AddColumn<int>(
                name: "InstallmentSequence",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentTransactionId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "RecurringTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "RecurringTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstallmentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FirstMonthAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: false),
                    Memo = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentTransactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentTransactions_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringSkips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecurringTransactionId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSkips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringSkips_RecurringTransactions_RecurringTransactionId",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InstallmentTransactionId",
                table: "Transactions",
                column: "InstallmentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentTransactions_CategoryId",
                table: "InstallmentTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentTransactions_PaymentMethodId",
                table: "InstallmentTransactions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSkips_RecurringTransactionId",
                table: "RecurringSkips",
                column: "RecurringTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_InstallmentTransactions_InstallmentTransaction~",
                table: "Transactions",
                column: "InstallmentTransactionId",
                principalTable: "InstallmentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_InstallmentTransactions_InstallmentTransaction~",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "InstallmentTransactions");

            migrationBuilder.DropTable(
                name: "RecurringSkips");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InstallmentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InstallmentSequence",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InstallmentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "RecurringTransactions");

            migrationBuilder.AddColumn<int>(
                name: "RemainingInstallments",
                table: "RecurringTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalInstallments",
                table: "RecurringTransactions",
                type: "integer",
                nullable: true);
        }
    }
}
