# ChatInsight

Платформа для интеллектуального анализа личных переписок. Импорт Telegram →
PostgreSQL → статистика, темы, эмоции, баланс отношений, сравнение периодов,
AI-инсайты, AI-портреты, **AI-хронология жизни**, PDF-отчёт. Веб-интерфейс с
графиками и асинхронными AI-блоками.

Две части: **ChatInsight.Api** (backend) и **chatinsight-web** (frontend).

---

## Стек

**Backend:** ASP.NET Core (.NET 9) · EF Core · PostgreSQL · Ollama (llama3.1) · QuestPDF
**Frontend:** React · TypeScript · Vite · TailwindCSS · Recharts

---

## Запуск

```bash
# backend
cd ChatInsight.Api
docker compose up -d
dotnet ef database update
ollama pull llama3.1:8b
dotnet run                    # http://localhost:5201

# frontend
cd chatinsight-web
npm install
npm run dev                   # http://localhost:5173
```

---

## Документация

- **[PROJECT_STATUS.md](PROJECT_STATUS.md)** · **[ROADMAP.md](ROADMAP.md)** · **[ARCHITECTURE.md](ARCHITECTURE.md)**
