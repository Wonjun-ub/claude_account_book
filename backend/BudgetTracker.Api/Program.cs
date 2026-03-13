using System.Text.Json.Serialization;
using BudgetTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── DbContext 등록 ─────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("'DefaultConnection' 연결 문자열이 설정되지 않았습니다.");

builder.Services.AddDbContext<BudgetTrackerDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── CORS 정책 ──────────────────────────────────────────────────────────────
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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
