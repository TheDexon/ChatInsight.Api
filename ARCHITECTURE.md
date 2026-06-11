# ChatInsight — ARCHITECTURE

Слои и потоки данных. Статус — `PROJECT_STATUS.md`, план — `ROADMAP.md`.

Система из двух частей: **ChatInsight.Api** (backend) и **chatinsight-web** (frontend),
связаны по HTTP (REST + CORS).

```text
chatinsight-web (React/Vite :5173)
        │  REST + CORS
        ▼
ChatInsight.Api (ASP.NET Core :5201)
        ├─ PostgreSQL (EF Core)
        └─ Ollama (:11434, локальная LLM)
```

---

## Пути данных (backend)

### Импорт (файл → БД, с дозагрузкой)

```text
POST /api/import/telegram
   → ReadExportAsync (валидация) → TelegramParser → TelegramExport
   → ChatImportService: ищет чат по SourceId;
        есть → добавляет только новые сообщения (по TelegramId), сбрасывает кэш AI
        нет  → создаёт новый чат
   → PostgreSQL → ответ: chatId, isNewChat, newMessages
```

### Анализ из БД (статистика / PDF / сравнение)

```text
GET /api/chats/{id}/report(.pdf) | /compare
   → ChatContextLoader → ChatAnalysisContext.Create (фильтр+сортировка, 1 раз)
   → ReportService / ComparisonService / PdfReportService → JSON | PDF
```

### AI (БД → Ollama → кэш в БД)

```text
GET /api/chats/{id}/insights | /personality
   → ChatContextLoader → ChatAnalysisContext
   → *CacheService: есть в БД и не refresh? → отдать мгновенно (X-Insight-Cache: hit)
                    иначе → *Service (Ollama, JSON Schema) → сохранить → отдать (miss)
   → Ollama недоступна → 503
```

Принцип: сообщения фильтруются/сортируются один раз в `ChatAnalysisContext.Create`.
Сервисы не знают, откуда данные (файл или БД).

---

## Backend: слои и папки

| Папка | Что лежит |
|---|---|
| `Models/Telegram/` | `TelegramExport` (+`Id`), `TelegramMessage` |
| `Models/Domain/` | сущности БД: `Chat` (+`SourceId`), `Message`, `ChatInsightRecord`, `PersonalityRecord` |
| `Parsers/` | `TelegramParser` |
| `Data/` | `ChatInsightDbContext`, `Migrations/` |
| `Domain/` | `ChatAnalysisContext` (рантайм-контекст) |
| `Services/Text/` | `TelegramTextExtractor`, `TextCleaner` |
| `Services/Import/` | `ChatImportService` |
| `Services/Analytics/` | `*Service`, `ReportService`, `ComparisonService`, `ChatContextLoader` |
| `Services/Ai/` | `OllamaClient`, `AiInsightService(+Cache)`, `PersonalityService(+Cache)` |
| `Reports/` | `PdfReportService` |
| `Analysis/<Модуль>/` | DTO результатов |
| `Controllers/` | по модулю + `ChatsController`, `AiController`, `PersonalityController`, `ComparisonController`, `ImportController` |
| `Configuration/` | `EmotionAnalysisOptions`, `OllamaOptions` |

### Соглашения по именам
- `ChatAnalysisContext` (`Domain/`) — рантайм-контекст анализа, НЕ база.
- `ChatInsightDbContext` (`Data/`) — EF Core `DbContext`.

---

## Модель БД

```text
Chat 1 ─< Many Message            (FK ChatId, cascade)
Chat 1 ─1 ChatInsightRecord       (uniq ChatId)
Chat 1 ─< Many PersonalityRecord  (uniq ChatId+Participant)

Chat:    Id(Guid), SourceId(long), Name, Type, ImportedAt, UpdatedAt?, MessageCount
Message: Id(long), ChatId, TelegramId, Type, Date(ts без tz), Author?, Text, RawTextJson?
Insights:      ChatId, Summary, EmotionalTone, Topics[], Dynamics[], Model, GeneratedAt
Personalities: ChatId, Participant, Summary, CommunicationStyle, Traits[], Model, GeneratedAt
```

`List<string>` (Topics, Dynamics, Traits) → нативный `text[]` Postgres (Npgsql).

---

## Frontend (chatinsight-web)

```text
src/
  api.ts            REST-клиент (axios, BASE_URL → :5201)
  types.ts          типы под ответы API
  components/
    Layout.tsx      шапка + навигация
    Charts.tsx      Recharts: активность по часам, по авторам
  pages/
    Upload.tsx      загрузка result.json
    ChatList.tsx    список чатов из БД
    ChatDetail.tsx  метрики, графики, AI-блоки (по кнопке), сравнение, PDF
```

Стиль: Spectral (display) + Inter (body) + JetBrains Mono (данные), индиго-акцент.
AI-блоки грузятся по нажатию (модель думает ~60с), повторно — мгновенно из кэша.

---

## Конфигурация

- `ConnectionStrings:Postgres` — БД (локально из `docker-compose.yml`; прод — env).
- `Ollama` — адрес/модель/таймаут/выборка. Модель: `llama3.1:8b`.
- `EmotionAnalysis` — словари. CORS `frontend` → `localhost:5173`.
- HTTPS-редирект включается только вне Development (в деве фронт ходит по http).
- EF: Npgsql + Design, мажор = таргету (`9.x` под `net9.0`).

---

## Точки расширения

- **Async AI:** `BackgroundService`/очередь, статус задачи, опрос с фронта.
- **AI-портреты в PDF:** `PdfReportService` принимает `List<PersonalityProfile>`.
- **pgvector:** расширение Postgres, векторное поле в `Message`, Npgsql.Pgvector.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message`.
