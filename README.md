# ChatInsight.Api

Backend для интеллектуального анализа переписок. Принимает экспорт Telegram
(`result.json`), сохраняет в PostgreSQL и выдаёт аналитику: статистику, темы,
эмоции, таймлайн, **AI-инсайты** (локальная модель) и **PDF-отчёт**.

**Стек:** ASP.NET Core (**.NET 9**) · PostgreSQL + EF Core · Ollama (llama3.1) · QuestPDF · Swagger.

---

## Быстрый старт

```bash
docker compose up -d          # Postgres
dotnet ef database update     # миграции (первый раз)
ollama pull llama3.1:8b       # AI-модель (один раз)
dotnet run
```

Swagger: `http://localhost:5201/swagger`.

---

## Основной флоу

1. `POST /api/import/telegram` — загрузить `result.json` → получить `chatId`.
2. `GET /api/chats/{id}/report` — сводный отчёт (JSON).
3. `GET /api/chats/{id}/report.pdf` — отчёт в PDF.
4. `GET /api/chats/{id}/insights` — AI-выводы (summary, тон, темы, динамика).

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** — что сделано и сверка с идеей.
- **[ROADMAP.md](ROADMAP.md)** — план (доводка MVP → фронт → v2/v3).
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — слои, пути данных, БД, AI.

---

## Статус

MVP из идеи закрыт, кроме графиков (они будут на фронте): импорт → PostgreSQL →
9 аналитических модулей → AI-инсайты → PDF. Дальше — кэш/асинхронность AI и фронтенд.
