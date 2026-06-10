# ChatInsight.Api — статус проекта

Backend для интеллектуального анализа переписок (пока — Telegram Export).
ASP.NET Core Web API на **.NET 9**, контроллеры + Swagger + **PostgreSQL (EF Core)**.

Срез текущего состояния: **что сделано**, **на чём остановились**, **что дальше**.
Полное видение продукта — в `1.docx`. Подробности по слоям — в `ARCHITECTURE.md`,
план развития — в `ROADMAP.md`.

---

## Что это сейчас

API принимает выгрузку Telegram (`result.json`), **сохраняет чат в PostgreSQL**
и возвращает `chatId`. Дальше анализ (статистика, текст, темы, эмоции, скорость
ответа, инициатива, таймлайн, отношения + сводный отчёт) собирается **из базы по
chatId** — без повторной загрузки файла. Анализ построен на статистике и словарях,
не на ML (AI — в плане).

---

## Что работает ✅

- [x] ASP.NET Core Web API (.NET 9), Swagger, HTTPS
- [x] Импорт и парсинг Telegram Export (`result.json`)
- [x] **PostgreSQL + EF Core**: сущности `Chat` / `Message`, миграции, сохранение импорта
- [x] **Импорт пишет в БД и возвращает `chatId`**
- [x] **Чтение из БД**: список чатов, метаданные, полный отчёт по `chatId`
- [x] `TelegramTextExtractor` — текст из `text`, когда это массив entity
- [x] `ChatAnalysisContext` — единый слой фильтрации/сортировки (все сервисы на нём)
- [x] Статистика активности (авторы / часы / дни / активный час / длина)
- [x] Текстовая аналитика и темы (частотность слов)
- [x] Эмоции (позитив / негатив / мат + toxicity), словари в конфиге
- [x] Скорость ответа, инициатива, таймлайн, отношения
- [x] Сводный отчёт (`/api/report` по файлу и `/api/chats/{id}/report` из БД)
- [x] Единая валидация загрузки (`AnalysisControllerBase`)
- [x] CORS-политика под фронт, guard от пустого чата
- [x] Docker Compose для Postgres

---

## Эндпоинты

| Маршрут | Метод | Источник | Что делает |
|---|---|---|---|
| `/api/import/telegram` | POST | файл → БД | сохраняет чат, возвращает `chatId` + метаданные |
| `/api/chats` | GET | БД | список сохранённых чатов |
| `/api/chats/{id}` | GET | БД | метаданные одного чата |
| `/api/chats/{id}/report` | GET | БД | полный отчёт по сохранённому чату |
| `/api/analysis/basic` | POST | файл | статистика (разовый прогон по файлу) |
| `/api/text`, `/api/topics`, `/api/emotion` | POST | файл | соответствующий модуль по файлу |
| `/api/response`, `/api/initiative` | POST | файл | по файлу |
| `/api/timeline`, `/api/relationship` | POST | файл | по файлу |
| `/api/report` | POST | файл | сводный отчёт по файлу |

> File-эндпоинты оставлены для разовых прогонов. Постепенно их можно перевести
> на `chatId` (см. ROADMAP) или убрать.

---

## Хранение данных

PostgreSQL, две таблицы (EF Core):

- **Chats** — `Id` (Guid), `Name`, `Type`, `ImportedAt`, `MessageCount`.
- **Messages** — `Id`, `ChatId` (FK, cascade), `TelegramId`, `Type`, `Date`
  (`timestamp without time zone`), `Author`, `Text` (плоский, для анализа),
  `RawTextJson` (оригинальный `text` как JSON — на будущее: ссылки, форматирование, AI).

Строка подключения — `ConnectionStrings:Postgres` в `appsettings.json` (локальный
dev-Postgres из `docker-compose.yml`). В проде переопределяется переменной
окружения `ConnectionStrings__Postgres`.

---

## Чего ещё нет ⛔

- [ ] **AI / LLM** (Ollama: Qwen/Gemma/Llama). Эмоции/темы — словари, не модель.
- [ ] **Экспорт отчёта в файл** (PDF/HTML/Markdown). Пока только JSON. Папка `Reports/` пустая.
- [ ] **Frontend** (React + TS + Tailwind).
- [ ] **Relationship**: `InitiativeBalance`/`ResponseBalance` пока не считаются.
- [ ] **pgvector** (семантика) — пакет Postgres есть, расширение не подключено.
- [ ] **Группы**: Relationship берёт топ-2 авторов, для групповых чатов логика не продумана.
- [ ] file-эндпоинты пока дублируют функциональность БД-эндпоинтов.

---

## Запуск

```bash
docker compose up -d          # поднять Postgres
dotnet ef database update     # применить миграции (первый раз)
dotnet run
```

Swagger: `http://localhost:5201/swagger`.
Флоу: `POST /api/import/telegram` → берёшь `chatId` → `GET /api/chats/{id}/report`.

> Стек EF: `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.x + `Microsoft.EntityFrameworkCore.Design` 9.0.x
> (мажор должен совпадать с таргетом `net9.0`).

---

## Коротко

Backend хранит импортированные чаты в PostgreSQL и собирает аналитику из базы по
`chatId` — полный цикл «импорт → хранение → анализ» работает end-to-end.
Следующие рубежи (см. `ROADMAP.md`): экспорт отчёта в PDF и AI на Ollama.

```text
[✓] Web API + Swagger
[✓] Импорт + парсинг Telegram
[✓] PostgreSQL + EF Core (Chat/Message, миграции)
[✓] Импорт → БД → chatId
[✓] Анализ из БД по chatId (/api/chats/{id}/report)
[✓] 9 аналитических модулей на ChatAnalysisContext

[ ] Экспорт отчёта в PDF / HTML / Markdown
[ ] AI Engine (Ollama)
[ ] Перевод всех эндпоинтов на chatId
[ ] Frontend (React + TS + Tailwind)
[ ] pgvector / Life Timeline Engine
```