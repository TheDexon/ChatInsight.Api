# ChatInsight

Платформа для интеллектуального анализа личных переписок. Импортирует экспорт
Telegram, сохраняет в PostgreSQL и выдаёт статистику, темы, эмоции, таймлайн,
сравнение периодов, **AI-инсайты**, **AI-портреты участников** и **PDF-отчёт** —
с веб-интерфейсом и графиками.

Репозиторий состоит из двух частей:

- **ChatInsight.Api** — backend (ASP.NET Core .NET 9, PostgreSQL, Ollama, QuestPDF).
- **chatinsight-web** — frontend (React + TypeScript + Vite + Tailwind).

---

## Стек

**Backend:** ASP.NET Core (.NET 9) · EF Core · PostgreSQL · Ollama (llama3.1) · QuestPDF · Swagger
**Frontend:** React · TypeScript · Vite · TailwindCSS · Recharts

---

## Запуск

### 1. Backend

```bash
cd ChatInsight.Api
docker compose up -d          # Postgres
dotnet ef database update     # миграции
ollama pull llama3.1:8b       # AI-модель (один раз)
dotnet run                    # http://localhost:5201 (Swagger: /swagger)
```

### 2. Frontend

```bash
cd chatinsight-web
npm install
npm run dev                   # http://localhost:5173
```

Бэкенд в Development работает по `http://localhost:5201` (HTTPS-редирект в деве
отключён), CORS настроен под `localhost:5173`.

---

## Что умеет

- Импорт Telegram с **дозагрузкой** новых сообщений в существующий чат
- 9 модулей статистической аналитики
- Сравнение периодов общения («было → стало»)
- AI-инсайты и AI-портреты участников (локальная модель, кэш в БД)
- PDF-отчёт с AI-резюме
- Веб-интерфейс: загрузка, список чатов, отчёт с графиками и AI-блоками

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** — что сделано и сверка с идеей.
- **[ROADMAP.md](ROADMAP.md)** — план развития.
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — слои, потоки данных, БД, AI, фронт.
