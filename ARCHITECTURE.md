# ChatInsight — ARCHITECTURE

Слои и потоки данных. Статус — `PROJECT_STATUS.md`, план — `ROADMAP.md`.

```text
chatinsight-web (React/Vite :5173)
        │  REST + CORS (+ опрос статуса задач)
        ▼
ChatInsight.Api (ASP.NET Core :5201)
        ├─ PostgreSQL (EF Core)
        ├─ AiJobWorker (BackgroundService) ←─ AiJobQueue (Channel)
        └─ Ollama (:11434, локальная LLM)
```

---

## Потоки данных

### Импорт (файл → БД, дозагрузка)
```text
POST /api/import/telegram → TelegramParser → ChatImportService
   (по SourceId: есть → добавить новые по TelegramId + сбросить кэш AI; нет → создать)
   → PostgreSQL → {chatId, isNewChat, newMessages}
```

### Анализ (БД → отчёт/PDF/сравнение)
```text
GET /api/chats/{id}/report(.pdf)|/compare
   → ChatContextLoader → ChatAnalysisContext.Create (фильтр+сортировка, 1 раз)
   → ReportService / ComparisonService / PdfReportService
```

### Асинхронный AI (очередь → воркер → БД → опрос)
```text
POST /api/chats/{id}/{insights|personality|lifetimeline}/async
   → AiJobService: дедуп (есть pending/running? вернуть его) → создать AiJob(pending)
                   → AiJobQueue.Enqueue(jobId) → вернуть {jobId} сразу
AiJobWorker (фон): читает очередь → свой DI-scope → ChatAnalysisContext
   → нужный CacheService.GetOrCreate (Ollama, JSON Schema) → ResultJson (camelCase) → done/failed
Фронт: POST → {jobId} → опрос GET /api/jobs/{jobId} каждые 2с → result | error
```

При старте воркер возвращает в очередь незавершённые задачи (переживание рестарта).

---

## Слои (backend)

| Папка | Что |
|---|---|
| `Models/Telegram/` · `Models/Domain/` | контракты ввода · сущности БД (Chat, Message, *Record, AiJob, LifeTimelineRecord) |
| `Parsers/` · `Data/` · `Domain/` | парсер · DbContext+миграции · ChatAnalysisContext |
| `Services/Text/` · `Services/Import/` | текст · импорт (upsert) |
| `Services/Analytics/` | аналитика, Report/Relationship/Comparison, ChatContextLoader |
| `Services/Ai/` | OllamaClient; Insight/Personality/LifeTimeline (+Cache); AiJobQueue/Service/Worker |
| `Reports/` | PdfReportService |
| `Analysis/<Модуль>/` · `Controllers/` · `Configuration/` | DTO · HTTP · опции |

---

## Модель БД

```text
Chat 1─< Message ; Chat 1─1 ChatInsightRecord ; Chat 1─< PersonalityRecord (uniq ChatId+Participant)
Chat 1─1 LifeTimelineRecord ; Chat 1─< AiJob

AiJob:            Id, ChatId, JobType(insights|personality|timeline), Status, ResultJson?, Error?, CreatedAt, CompletedAt?
LifeTimelineRecord: Id, ChatId(uniq), EventsJson, Summary, Model, GeneratedAt
```

Списки (Topics/Dynamics/Traits) → нативный `text[]`. События хронологии — JSON-строкой.

---

## Frontend (chatinsight-web)

```text
src/api.ts (REST + pollJob) · types.ts
src/components/  Layout.tsx · Charts.tsx (часы/авторы/дни)
src/pages/       Upload · ChatList · ChatDetail (метрики, графики, баланс отношений,
                 эмоции, async AI-блоки: инсайты/портреты/хронология, сравнение, PDF)
```
Async-блоки: кнопка → спиннер со статусом (в очереди/думает) → результат. Опрос `pollJob`.

---

## AI-слой (детали)

- `OllamaClient` — `IHttpClientFactory`, JSON Schema в `format` (движок гарантирует структуру).
- Кэш-сервисы (`*CacheService`) — единый паттерн get-or-create + read-only `GetCached`.
- Воркер сериализует результат **camelCase** → совпадает с тем, что отдают синхронные
  эндпоинты и ждёт фронт.
- Недоступна Ollama → задача `failed` с текстом ошибки; синхронные эндпоинты → 503.

---

## Точки расширения / конфиг

- **Эволюция личности:** сравнить портреты по периодам (есть Comparison + Personality).
- **pgvector:** расширение Postgres, векторное поле в `Message`.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message`.
- Конфиг: `ConnectionStrings:Postgres` · `Ollama` (`llama3.1:8b`) · `EmotionAnalysis` ·
  CORS→:5173 · HTTPS-редирект только вне Development · EF Npgsql, мажор=`net9.0`.
