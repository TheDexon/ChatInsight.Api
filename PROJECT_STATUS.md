# ChatInsight — статус проекта

Платформа анализа переписок: backend (**ASP.NET Core .NET 9** + PostgreSQL + Ollama)
и frontend (**React + Vite + Tailwind**).

Срез: что сделано, на чём остановились, что дальше. Идея — в `1.docx`.
Слои — `ARCHITECTURE.md`, план — `ROADMAP.md`.

---

## Сверка с идеей (MVP 1.0)

| Пункт MVP | Статус |
|---|---|
| Импорт Telegram Export | ✅ (+ дозагрузка новых сообщений) |
| Сохранение сообщений | ✅ PostgreSQL + EF Core |
| Анализ активности | ✅ |
| Анализ скорости ответов | ✅ |
| Графики | ✅ на фронте (Recharts) |
| Базовый AI-отчёт | ✅ Ollama / llama3.1 |
| Экспорт PDF | ✅ QuestPDF (+ AI-резюме) |

**MVP 1.0 закрыт полностью.** Сверх MVP: темы, эмоции, инициатива, таймлайн,
отношения, сравнение периодов, AI-портреты участников, веб-интерфейс.

> Отличия от стека идеи: `net9.0` вместо `.NET 10`; `pgvector` пока не подключён (v2.0).

---

## Что работает ✅

### Backend
- [x] Web API (.NET 9), Swagger, PostgreSQL + EF Core, Docker Compose
- [x] Импорт Telegram с дозагрузкой (по `SourceId` + `TelegramId`), сброс кэша AI
- [x] Чтение из БД по `chatId`
- [x] 9 аналитических модулей на едином `ChatAnalysisContext`
- [x] Сравнение периодов («было → стало»)
- [x] AI-инсайты и AI-портреты участников (Ollama, JSON Schema, кэш в БД)
- [x] PDF-отчёт (QuestPDF) с опциональной AI-секцией
- [x] HTTPS-редирект отключён в Development (фронт ходит по http)

### Frontend (chatinsight-web)
- [x] Загрузка `result.json` (страница `/upload`)
- [x] Список чатов из БД (`/`)
- [x] Страница анализа (`/chat/:id`): метрики, графики (часы, авторы),
      скачивание PDF, AI-блоки (инсайты, портреты, сравнение) по кнопке
- [x] CORS-связка с бэкендом, обработка ошибок и пустых состояний

---

## Эндпоинты API

| Маршрут | Метод | Что делает |
|---|---|---|
| `/api/import/telegram` | POST | импорт/дозагрузка → `chatId`, `newMessages`, `isNewChat` |
| `/api/chats` · `/api/chats/{id}` | GET | список / метаданные |
| `/api/chats/{id}/report` · `.pdf` | GET | отчёт JSON / PDF (`?ai=true`) |
| `/api/chats/{id}/insights` | GET | AI-выводы (кэш, `?refresh=true`) |
| `/api/chats/{id}/personality` | GET | AI-портреты (кэш, `?refresh=true`) |
| `/api/chats/{id}/compare` | GET | сравнение периодов (`?splitDate=...`) |

---

## Хранение

PostgreSQL, EF Core. Таблицы: **Chats** (+`SourceId`), **Messages**,
**Insights** (кэш инсайтов, 1 на чат), **Personalities** (кэш портретов, 1 на
пару чат+участник). Строка подключения — `ConnectionStrings:Postgres`.

---

## Чего ещё нет ⛔

- [ ] **Асинхронный AI** (запустил → забрал позже) — фронт сейчас ждёт ответ синхронно.
- [ ] **Relationship**: `InitiativeBalance` / `ResponseBalance` не считаются.
- [ ] **AI-портреты внутри PDF** (пока только в вебе и JSON).
- [ ] **pgvector**, Life Timeline, эволюция личности — v2.0.
- [ ] Группы, мультиплатформа (Discord/WhatsApp/VK), SaaS — v3.0.
- [ ] Старые чаты (до `SourceId`) имеют `SourceId=0` — не склеятся при дозагрузке.
- [ ] Фронт — рабочий MVP-каркас (без пагинации, авторизации, вылизанной вёрстки).

---

## Коротко

```text
[✓] Импорт (+дозагрузка) + PostgreSQL
[✓] 9 модулей аналитики + сравнение периодов
[✓] AI-инсайты + AI-портреты (кэш в БД)
[✓] PDF-отчёт с AI
[✓] Frontend: React + графики + AI-блоки   ← MVP закрыт

[ ] Async AI · AI-портреты в PDF
[ ] pgvector / эволюция личности (v2)
[ ] Группы / мультиплатформа / SaaS (v3)
```
