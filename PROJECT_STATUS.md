# ChatInsight.Api — статус проекта

Backend для интеллектуального анализа переписок (Telegram Export).
ASP.NET Core Web API на **.NET 9** + **PostgreSQL (EF Core)** + **AI (Ollama)**.

Срез состояния: **что сделано**, **на чём остановились**, **что дальше**.
Полное видение — в `1.docx`. Слои — в `ARCHITECTURE.md`, план — в `ROADMAP.md`.

---

## Сверка с идеей (MVP 1.0 из `1.docx`)

| Пункт MVP | Статус |
|---|---|
| Импорт Telegram Export | ✅ |
| Сохранение сообщений | ✅ PostgreSQL + EF Core |
| Анализ активности | ✅ |
| Анализ скорости ответов | ✅ |
| Графики | ⛔ нет (будут на фронте) |
| Базовый AI-отчёт | ✅ Ollama / llama3.1 |
| Экспорт PDF | ✅ QuestPDF |

**MVP закрыт, кроме графиков** (они логически относятся к фронтенду). Сверх MVP
уже сделаны темы, эмоции, инициатива, таймлайн, отношения.

> Отличия от стека в идее: заявлен `.NET 10` — фактически `net9.0` (EF 9, Npgsql 9).
> `pgvector` из стека пока не подключён (это Версия 2.0).

---

## Что это сейчас

Импорт Telegram (`result.json`) → **сохранение в PostgreSQL** → анализ из базы по
`chatId`. Доступно: статистическая аналитика (9 модулей), **AI-инсайты через
локальную модель** и **экспорт отчёта в PDF**. Полный цикл «импорт → хранение →
анализ → отчёт» работает end-to-end.

---

## Что работает ✅

- [x] ASP.NET Core Web API (.NET 9), Swagger
- [x] Импорт и парсинг Telegram Export
- [x] PostgreSQL + EF Core: `Chat` / `Message`, миграции, Docker Compose
- [x] Импорт пишет в БД и возвращает `chatId`
- [x] Чтение из БД: список чатов, метаданные, отчёт по `chatId`
- [x] 9 аналитических модулей на едином `ChatAnalysisContext`
      (статистика, текст, темы, эмоции, скорость ответа, инициатива, таймлайн, отношения, сводный отчёт)
- [x] **AI-инсайты (Ollama / llama3.1)**: summary, эмоциональный тон, темы, динамика
      — через structured output (JSON Schema)
- [x] **Экспорт отчёта в PDF (QuestPDF)**
- [x] Единая валидация загрузки, CORS, guard от пустого чата

---

## Эндпоинты

| Маршрут | Метод | Источник | Что делает |
|---|---|---|---|
| `/api/import/telegram` | POST | файл → БД | сохраняет чат, возвращает `chatId` |
| `/api/chats` | GET | БД | список сохранённых чатов |
| `/api/chats/{id}` | GET | БД | метаданные чата |
| `/api/chats/{id}/report` | GET | БД | сводный отчёт (JSON) |
| `/api/chats/{id}/report.pdf` | GET | БД | отчёт в PDF |
| `/api/chats/{id}/insights` | GET | БД + AI | AI-выводы по чату (Ollama) |
| `/api/analysis/basic`, `/api/text`, `/api/topics`, … | POST | файл | разовые прогоны по файлу |

---

## Хранение данных

PostgreSQL, EF Core:
- **Chats** — `Id`(Guid), `Name`, `Type`, `ImportedAt`, `MessageCount`.
- **Messages** — `Id`, `ChatId`(FK, cascade), `TelegramId`, `Type`, `Date`
  (`timestamp without time zone`), `Author`, `Text` (плоский), `RawTextJson` (оригинал).

Строка подключения — `ConnectionStrings:Postgres` (`appsettings.json`), локальный
Postgres из `docker-compose.yml`. Прод — env `ConnectionStrings__Postgres`.

---

## AI-слой

- **Ollama** локально (`http://localhost:11434`), модель в конфиге (`Ollama:Model`).
- Рабочая модель — **`llama3.1:8b`**: хорошо держит русский, не морализирует,
  не уплывает в другой язык. (`qwen2.5:14b` на structured-режиме сваливалась в
  китайский и цензурировала грубый контент — для этой задачи не подошла.)
- Ответ гарантируется **JSON Schema** (движок заполняет все поля).
- Ollama недоступна → эндпоинт отдаёт **503** с понятным сообщением, не падает.
- В промпт идёт равномерная выборка сообщений (`Ollama:SampleMessages`), не вся переписка.

---

## Чего ещё нет ⛔

- [ ] **Графики** (последний пункт MVP) — на фронтенде.
- [ ] **Frontend** (React + TS + Tailwind).
- [ ] **Кэш AI-инсайтов в БД** — сейчас модель считает заново каждый раз (60+ сек).
- [ ] **Асинхронный AI** (запустил → забрал результат позже).
- [ ] **AI-резюме внутри PDF** — пока PDF и инсайты раздельно.
- [ ] **Relationship**: `InitiativeBalance` / `ResponseBalance` не считаются.
- [ ] **pgvector**, групповой анализ, мультиплатформенность — Версии 2.0–3.0.

---

## Запуск

```bash
docker compose up -d            # Postgres
dotnet ef database update       # миграции (первый раз)
ollama pull llama3.1:8b         # модель (один раз)
dotnet run
```

Swagger: `http://localhost:5201/swagger`.
Флоу: `POST /api/import/telegram` → `chatId` → `GET /api/chats/{id}/report.pdf` или `/insights`.

---

## Коротко

MVP из идеи закрыт (кроме графиков): импорт → PostgreSQL → аналитика → AI-инсайты
→ PDF. Дальше — фронтенд (и графики на нём), кэш/асинхронность AI, затем Версия 2.0
(личность, сравнение периодов, pgvector). См. `ROADMAP.md`.

```text
[✓] Web API + Swagger
[✓] Импорт + PostgreSQL (EF Core)
[✓] Анализ по chatId (9 модулей)
[✓] AI-инсайты (Ollama / llama3.1, JSON Schema)
[✓] PDF-отчёт (QuestPDF)

[ ] Графики / Frontend (React + TS + Tailwind)
[ ] Кэш AI-инсайтов + асинхронность
[ ] AI-резюме в PDF
[ ] pgvector / личность / сравнение периодов (v2)
[ ] Группы / мультиплатформа / SaaS (v3)
```
