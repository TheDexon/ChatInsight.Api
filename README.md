# ChatInsight.Api

Backend для интеллектуального анализа переписок. Принимает экспорт Telegram
(`result.json`) и считает статистику, темы, эмоции, скорость ответа, инициативу,
таймлайн и отношения — отдаёт результат как JSON через REST API.

**Стек:** ASP.NET Core Web API на **.NET 10**, контроллеры, Swagger.
Пока всё считается в памяти за один запрос — без базы и без AI (это в плане).

---

## Быстрый старт

```bash
dotnet build
dotnet run
```

Swagger: `https://localhost:7015/swagger` (или `http://localhost:5201`).
Грузи `result.json` (Telegram → Экспорт истории чата → JSON) в любой POST-эндпоинт.

> Нужен **.NET 10 SDK** (`dotnet --list-sdks`). Если стоит только 9.x —
> поменяй `<TargetFramework>` в `ChatInsight.Api.csproj` на `net9.0`.

---

## Эндпоинты

| Маршрут | Что делает |
|---|---|
| `POST /api/import/telegram` | метаданные чата (имя, участники, даты, кол-во) |
| `POST /api/analysis/basic` | статистика активности |
| `POST /api/text` | текстовая аналитика (топ-слова и т.д.) |
| `POST /api/topics` | темы (частотность слов) |
| `POST /api/emotion` | эмоции и toxicity |
| `POST /api/response` | скорость ответа по авторам |
| `POST /api/initiative` | кто чаще начинает диалог |
| `POST /api/timeline` | события: начало, пики, паузы, всплески |
| `POST /api/relationship` | баланс активности, доминирующий участник |
| `POST /api/report` | всё сразу одним отчётом |

Все принимают `IFormFile file` (`result.json`), `multipart/form-data`.

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** — что сделано, на чём остановились, что дальше.
- **[ROADMAP.md](ROADMAP.md)** — план развития (БД → AI → продукт).
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — слои, поток данных, соглашения по папкам.

---

## Статус

MVP-аналитика готова и собирается. Следующий рубеж — база данных (PostgreSQL +
EF Core), сохранение импорта, экспорт отчёта в PDF, затем AI на Ollama.
Подробнее — в `ROADMAP.md`.