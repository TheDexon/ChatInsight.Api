# ChatInsight.Api

Backend для интеллектуального анализа переписок. Принимает экспорт Telegram
(`result.json`), сохраняет в PostgreSQL и выдаёт аналитику: статистику, темы,
эмоции, таймлайн, сравнение периодов, **AI-инсайты**, **AI-портреты участников**
и **PDF-отчёт**.

**Стек:** ASP.NET Core (**.NET 9**) · PostgreSQL + EF Core · Ollama (llama3.1) · QuestPDF · Swagger.

---

## Быстрый старт

```bash
docker compose up -d          # Postgres
dotnet ef database update     # миграции
ollama pull llama3.1:8b       # AI-модель (один раз)
dotnet run
```

Swagger: `http://localhost:5201/swagger`.

---

## Основной флоу

1. `POST /api/import/telegram` — загрузить `result.json` → `chatId`
   (повторный импорт того же чата **дополняет** его новыми сообщениями).
2. `GET /api/chats/{id}/report` — сводный отчёт (JSON).
3. `GET /api/chats/{id}/report.pdf` — отчёт в PDF (`?ai=true` — с AI-резюме).
4. `GET /api/chats/{id}/insights` — AI-выводы (summary, тон, темы, динамика).
5. `GET /api/chats/{id}/personality` — AI-портреты участников.
6. `GET /api/chats/{id}/compare` — сравнение периодов («было → стало»).

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** — что сделано и сверка с идеей.
- **[ROADMAP.md](ROADMAP.md)** — план развития.
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — слои, пути данных, БД, AI.

---

## Статус

MVP закрыт, кроме графиков (фронтенд). Сделано: импорт с дозагрузкой → PostgreSQL →
аналитика → AI-инсайты, AI-портреты, сравнение периодов → PDF с AI. Дальше — фронтенд.
