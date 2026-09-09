# Syncora

Приложение для совместного планирования: календари, встречи, списки покупок и уведомления.

## Архитектура

```
Avalonia Client → REST API → Services → EF Core / DbContext → PostgreSQL
```

## Структура проекта

```
Syncora/
├── src/
│   ├── Syncora.Api/       # ASP.NET Core Web API
│   └── Syncora.Client/    # Avalonia desktop client
├── tests/
│   ├── Syncora.Api.Tests/
│   └── Syncora.IntegrationTests/
├── docker-compose.yml
└── Syncora.sln
```

## Требования

- .NET 8 SDK (API)
- .NET 10 SDK (Client)
- PostgreSQL 16 (или Docker)

## Запуск

### База данных

```bash
docker compose up -d postgres
```

### API

```bash
dotnet run --project src/Syncora.Api
```

Swagger: http://localhost:5131/swagger

### Client

```bash
dotnet run --project src/Syncora.Client
```

## Тесты

```bash
dotnet test
```
