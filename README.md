# ChatInsight

Платформа для интеллектуального анализа личных переписок. Импортирует экспорт
Telegram, сохраняет в PostgreSQL и выдаёт статистику, темы, эмоции, таймлайн,
баланс отношений, сравнение периодов, **AI-инсайты**, **AI-портреты участников**
и **PDF-отчёт** (с AI-анализом и портретами внутри) — с веб-интерфейсом и графиками.

Две части:
- **ChatInsight.Api** — backend (ASP.NET Core .NET 9, PostgreSQL, Ollama, QuestPDF).
- **chatinsight-web** — frontend (React + TypeScript + Vite + Tailwind).

---

## Стек

**Backend:** ASP.NET Core (.NET 9) · EF Core · PostgreSQL · Ollama (llama3.1) · QuestPDF · Swagger
**Frontend:** React · TypeScript · Vite · TailwindCSS · Recharts

---

## Запуск

### Backend
```bash
cd ChatInsight.Api
docker compose up -d
dotnet ef database update
ollama pull llama3.1:8b
dotnet run                    # http://localhost:5201 (Swagger: /swagger)
```

### Frontend
```bash
cd chatinsight-web
npm install
npm run dev                   # http://localhost:5173
```

В Development бэкенд работает по http (HTTPS-редирект отключён), CORS — под :5173.

---

## Что умеет

- Импорт Telegram с дозагрузкой новых сообщений
- 9 модулей статистической аналитики + баланс отношений
- Сравнение периодов («было → стало»)
- AI-инсайты и AI-портреты участников (локальная модель, кэш в БД)
- PDF-отчёт с AI-резюме и портретами участников
- Веб-интерфейс: загрузка, список чатов, отчёт с графиками и AI-блоками

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** · **[ROADMAP.md](ROADMAP.md)** · **[ARCHITECTURE.md](ARCHITECTURE.md)**
