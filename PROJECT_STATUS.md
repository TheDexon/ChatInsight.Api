# ChatInsight.Api — статус проекта

Backend для анализа переписок (Telegram Export).
ASP.NET Core **.NET 9** + **PostgreSQL (EF Core)** + **AI (Ollama / llama3.1)**.

Срез: что сделано, на чём остановились, что дальше. Идея — в `1.docx`.
Слои — в `ARCHITECTURE.md`, план — в `ROADMAP.md`.

---

## Сверка с идеей (MVP 1.0)

| Пункт MVP | Статус |
|---|---|
| Импорт Telegram Export | ✅ (+ дозагрузка новых сообщений) |
| Сохранение сообщений | ✅ PostgreSQL + EF Core |
| Анализ активности | ✅ |
| Анализ скорости ответов | ✅ |
| Графики | ⛔ нет (на фронтенде) |
| Базовый AI-отчёт | ✅ Ollama / llama3.1 |
| Экспорт PDF | ✅ QuestPDF (+ AI-резюме внутри) |

**MVP закрыт, кроме графиков.** Сверх MVP: темы, эмоции, инициатива, таймлайн,
отношения, **сравнение периодов**, **AI-портреты участников**.

> Отличия от стека идеи: `net9.0` вместо `.NET 10`; `pgvector` пока не подключён (v2.0).

---

## Что работает ✅

- [x] Web API (.NET 9), Swagger, PostgreSQL + EF Core, Docker Compose
- [x] Импорт Telegram с **дозагрузкой**: повторный импорт того же чата (по `SourceId`)
      добавляет только новые сообщения (по `TelegramId`) и сбрасывает кэш AI
- [x] Чтение из БД по `chatId`: список, метаданные, отчёт
- [x] 9 аналитических модулей на едином `ChatAnalysisContext`
- [x] **Сравнение периодов** («было → стало»: активность, токсичность, темы, скорость)
- [x] **AI-инсайты** (summary, тон, темы, динамика) — кэш в БД
- [x] **AI-портреты участников** (характер, стиль, черты) — кэш в БД
- [x] **PDF-отчёт** (QuestPDF) с опциональной AI-секцией
- [x] Все AI-вызовы: structured output (JSON Schema), 503 при недоступности Ollama

---

## Эндпоинты

| Маршрут | Метод | Что делает |
|---|---|---|
| `/api/import/telegram` | POST | импорт/дозагрузка, возвращает `chatId`, `newMessages`, `isNewChat` |
| `/api/chats` · `/api/chats/{id}` | GET | список / метаданные |
| `/api/chats/{id}/report` · `.pdf` | GET | отчёт JSON / PDF (`?ai=true`) |
| `/api/chats/{id}/insights` | GET | AI-выводы (кэш, `?refresh=true`) |
| `/api/chats/{id}/personality` | GET | AI-портреты участников (кэш, `?refresh=true`) |
| `/api/chats/{id}/compare` | GET | сравнение периодов (`?splitDate=...`) |
| `/api/analysis/basic`, `/api/text`, … | POST | разовые прогоны по файлу |

---

## Хранение

PostgreSQL, EF Core. Таблицы:
- **Chats** — `Id`, `SourceId` (telegram id чата), `Name`, `Type`, `ImportedAt`, `UpdatedAt`, `MessageCount`.
- **Messages** — `Id`, `ChatId`(FK), `TelegramId`, `Type`, `Date`, `Author`, `Text`, `RawTextJson`.
- **Insights** — кэш AI-инсайтов (1 на чат).
- **Personalities** — кэш AI-портретов (1 на пару чат+участник).

Строка подключения — `ConnectionStrings:Postgres`; прод — env `ConnectionStrings__Postgres`.

---

## AI-слой

- Ollama локально, модель в конфиге (`Ollama:Model`). Рабочая — **`llama3.1:8b`**
  (русский, без морализаторства; `qwen2.5:14b` сваливалась в китайский — непригодна).
- Ответы гарантируются **JSON Schema**. Недоступна → 503, не падаем.
- Тяжёлые AI-результаты (инсайты, портреты) **кэшируются в БД** — считаются один раз.

---

## Чего ещё нет ⛔

- [ ] **Графики / Frontend** (React + TS + Tailwind) — последний пункт MVP.
- [ ] **Асинхронный AI** (запустил → забрал позже) — нужен с фронтом.
- [ ] **Relationship**: `InitiativeBalance` / `ResponseBalance` не считаются.
- [ ] **pgvector**, Life Timeline, групповой анализ, мультиплатформа — v2.0–3.0.
- [ ] Старые чаты (импортированные до `SourceId`) имеют `SourceId=0` — не склеятся при дозагрузке.

---

## Запуск

```bash
docker compose up -d
dotnet ef database update
ollama pull llama3.1:8b
dotnet run
```

Swagger: `http://localhost:5201/swagger`.

---

## Коротко

```text
[✓] Импорт (+дозагрузка) + PostgreSQL
[✓] 9 модулей аналитики + сравнение периодов
[✓] AI-инсайты + AI-портреты (кэш в БД)
[✓] PDF-отчёт с AI

[ ] Frontend + графики
[ ] Async AI
[ ] pgvector / v2.0+
```
