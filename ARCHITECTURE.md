# ChatInsight.Api — ARCHITECTURE

Слои и потоки данных. Статус — `PROJECT_STATUS.md`, план — `ROADMAP.md`.

---

## Пути данных

### Импорт (файл → БД, с дозагрузкой)

```text
POST /api/import/telegram (result.json)
   → ReadExportAsync (валидация) → TelegramParser → TelegramExport
   → ChatImportService:
        • ищет чат по SourceId (telegram id чата)
        • есть → добавляет только новые сообщения (по TelegramId), сбрасывает кэш AI
        • нет  → создаёт новый чат
   → PostgreSQL (Chats, Messages)
   → ответ: chatId, isNewChat, newMessages
```

### Анализ из БД (статистика / PDF / сравнение)

```text
GET /api/chats/{id}/report(.pdf) | /compare
   → ChatContextLoader.LoadAsync(chatId) (Messages → TelegramExport)
   → ChatAnalysisContext.Create (фильтр+сортировка, 1 раз)
   → ReportService / ComparisonService / PdfReportService → JSON | PDF
```

### AI (БД → Ollama → кэш в БД)

```text
GET /api/chats/{id}/insights | /personality
   → ChatContextLoader → ChatAnalysisContext
   → *CacheService: есть в БД и не refresh? → отдать мгновенно
                    иначе → *Service (Ollama, JSON Schema) → сохранить → отдать
   → Ollama недоступна → 503
```

Принцип: сообщения фильтруются/сортируются один раз в `ChatAnalysisContext.Create`.
Все сервисы (аналитика, сравнение, PDF, AI) работают с `ChatAnalysisContext` и не
знают, откуда данные.

---

## Слои и папки

| Папка | Что лежит |
|---|---|
| `Models/Telegram/` | `TelegramExport` (+`Id` чата), `TelegramMessage` |
| `Models/Domain/` | сущности БД: `Chat` (+`SourceId`), `Message`, `ChatInsightRecord`, `PersonalityRecord` |
| `Parsers/` | `TelegramParser` |
| `Data/` | `ChatInsightDbContext`, `Migrations/` |
| `Domain/` | `ChatAnalysisContext` (рантайм-контекст анализа) |
| `Services/Text/` | `TelegramTextExtractor`, `TextCleaner` |
| `Services/Import/` | `ChatImportService` (upsert + дозагрузка) |
| `Services/Analytics/` | `*Service`, `ReportService`, `ComparisonService`, `ChatContextLoader` |
| `Services/Ai/` | `OllamaClient`, `AiInsightService(+Cache)`, `PersonalityService(+Cache)` |
| `Reports/` | `PdfReportService` (QuestPDF, опц. AI-секция) |
| `Analysis/<Модуль>/` | DTO результатов (`*Statistics`, `AiInsight`, `PersonalityProfile`, `PeriodComparison`) |
| `Controllers/` | по модулю + `ChatsController`, `AiController`, `PersonalityController`, `ComparisonController`, `ImportController` |
| `Configuration/` | `EmotionAnalysisOptions`, `OllamaOptions` |

---

## Соглашения по именам

- **`ChatAnalysisContext`** (`Domain/`) — рантайм-контекст анализа, НЕ база.
- **`ChatInsightDbContext`** (`Data/`) — EF Core `DbContext`.
- EF-сущности — в `Models/Domain/`.

---

## Модель БД

```text
Chat 1 ─< Many Message            (FK ChatId, cascade)
Chat 1 ─1 ChatInsightRecord       (uniq ChatId)
Chat 1 ─< Many PersonalityRecord  (uniq ChatId+Participant)

Chat:    Id(Guid), SourceId(long, telegram id), Name, Type, ImportedAt, UpdatedAt?, MessageCount
Message: Id(long), ChatId, TelegramId, Type, Date(ts без tz), Author?, Text, RawTextJson?
Insights:       ChatId, Summary, EmotionalTone, Topics[], Dynamics[], Model, GeneratedAt
Personalities:  ChatId, Participant, Summary, CommunicationStyle, Traits[], Model, GeneratedAt
```

`SourceId` — распознать «тот же» чат при повторном импорте. `List<string>` (Topics,
Dynamics, Traits) → нативный `text[]` Postgres (Npgsql, без конвертеров).

---

## Кэш AI

`AiInsightCacheService` и `PersonalityCacheService` — единый паттерн: при запросе
смотрим БД, есть → отдаём (заголовок `X-Insight-Cache: hit`), нет → считаем моделью
и сохраняем (`miss`). `?refresh=true` — пересчёт. Дозагрузка новых сообщений в чат
сбрасывает кэш инсайтов.

---

## Как добавить аналитический модуль

1. DTO → `Analysis/<Модуль>/`. 2. Сервис → `Analyze(ChatAnalysisContext)`.
3. DI в `Program.cs`. 4. Эндпоинт: файл → `AnalysisControllerBase`; БД → `ChatContextLoader`.
5. (Опц.) в `ReportService` / PDF.

---

## Точки расширения

- **Async AI:** `BackgroundService`/очередь, статус задачи.
- **AI-портреты в PDF:** `PdfReportService` принимает `List<PersonalityProfile>`.
- **pgvector:** расширение Postgres, векторное поле в `Message`, Npgsql.Pgvector.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message`.

---

## Конфигурация

`ConnectionStrings:Postgres` · `Ollama` (адрес/модель/таймаут/выборка) ·
`EmotionAnalysis` (словари) · CORS `frontend`. EF: Npgsql + Design, мажор = `net9.0`.
