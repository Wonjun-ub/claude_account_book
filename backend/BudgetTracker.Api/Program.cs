using System.Text.Json.Serialization;
using BudgetTracker.Api.Data;
using BudgetTracker.Api.Repositories;
using BudgetTracker.Api.Repositories.Interfaces;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── DbContext 등록 ─────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("'DefaultConnection' 연결 문자열이 설정되지 않았습니다.");

builder.Services.AddDbContext<BudgetTrackerDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── CORS 정책 (MVP: 인증 추가 전까지 전체 허용) ────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Repositories ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPointBudgetRepository, PointBudgetRepository>();
builder.Services.AddScoped<IRecurringRepository, RecurringRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<IPointBudgetService, PointBudgetService>();
builder.Services.AddScoped<IRecurringService, RecurringService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();

// ── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum을 문자열로 직렬화
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // null 값 제외
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BudgetTracker API", Version = "v1" });
});

var app = builder.Build();

// ── Swagger UI (전 환경 활성화) ────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BudgetTracker API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("DefaultCors");
app.UseAuthorization();
app.MapControllers();

app.Run();
