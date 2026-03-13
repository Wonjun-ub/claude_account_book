# ── 빌드 스테이지 ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BudgetTracker.sln ./
COPY BudgetTracker.Api/BudgetTracker.Api.csproj BudgetTracker.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish BudgetTracker.Api/BudgetTracker.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── 런타임 스테이지 ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render는 PORT 환경변수로 포트를 전달함
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}

ENTRYPOINT ["dotnet", "BudgetTracker.Api.dll"]
