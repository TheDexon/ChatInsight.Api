# ChatInsight — ARCHITECTURE

Слои и потоки данных. Статус — `PROJECT_STATUS.md`, план — `ROADMAP.md`.

```text
chatinsight-web (React/Vite :5173)
        │  REST + CORS (+ опрос статуса задач)
        ▼
ChatInsight.Api (ASP.NET Core :5201)
        ├─ PostgreSQL (EF Core)
        ├─ AiJobWorker (BackgroundService) ←─ AiJobQueue (Channel)
        └─ Ollama (:11434): llama3.1 (анализ) + nomic-embed-text (векторы)
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
POST /api/chats/{id}/{insights|personality|lifetimeline|evolution|embeddings|clusters|rollup}/async
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
| `Services/Text/` · `Services/Import/` | текст, MeaningfulTextFilter · импорт (upsert) |
| `Services/Analytics/` | аналитика, Report/Relationship/Comparison, ChatContextLoader |
| `Services/Ai/` | OllamaClient/EmbeddingClient; Insight/Personality/Timeline/Evolution (+Cache); Embedding/SemanticSearch/TopicCluster (k-means); DigestService (выжимки-фундамент)/RollupCache; AiJobQueue/Service/Worker |
| `Reports/` | PdfReportService |
| `Analysis/<Модуль>/` · `Controllers/` · `Configuration/` | DTO · HTTP · опции |

---

## Модель БД

```text
Chat 1─< Message ; Chat 1─1 ChatInsightRecord ; Chat 1─< PersonalityRecord (uniq ChatId+Participant)
Chat 1─1 LifeTimelineRecord ; Chat 1─< AiJob

AiJob:            Id, ChatId, JobType(insights|personality|timeline|evolution|embeddings|clusters|rollup), Status, ResultJson?, Error?, CreatedAt, CompletedAt?
LifeTimelineRecord: Id, ChatId(uniq), EventsJson, Summary, Model, GeneratedAt
```

Списки (Topics/Dynamics/Traits) → нативный `text[]`. События хронологии — JSON-строкой.

---

## Frontend (chatinsight-web)

```text
src/api.ts (REST + pollJob) · types.ts
src/components/  Layout.tsx · Charts.tsx (часы/авторы/дни)
src/pages/       Upload · ChatList · ChatDetail (метрики, графики, баланс отношений,
                 эмоции, async AI-блоки: инсайты/портреты/хронология/эволюция, сравнение, PDF)
```
Async-блоки: кнопка → спиннер со статусом (в очереди/думает) → результат. Опрос `pollJob`.

---

## AI-слой (детали)

- `OllamaClient` — `IHttpClientFactory`, JSON Schema в `format` (движок гарантирует структуру).
- Кэш-сервисы (`*CacheService`) — единый паттерн get-or-create + read-only `GetCached`.
- Воркер сериализует результат **camelCase**. Все AI-промпты включают AiPrompts.IronyNote
  (общение ироничное). Эмбеддинги/кластеры строятся только по осмысленным сообщениям.
  Пересчёт: POST `…/async?refresh=true` сбрасывает кэш нужного типа.
- Недоступна Ollama → задача `failed` с текстом ошибки; синхронные эндпоинты → 503.

---

## Семантический поиск (pgvector)

```text
POST /chats/{id}/embeddings/async → фоном: для каждого сообщения Ollama /api/embeddings
   (nomic-embed-text → 768) → MessageEmbeddings.Embedding (vector(768))
GET  /chats/{id}/search?q=... → embed(запрос) → ORDER BY Embedding <=> qv (косинус) → top-N
```
EF: `UseVector()` в Program.cs; `HasPostgresExtension("vector")`; колонка `vector(768)`;
образ БД `pgvector/pgvector:pg16`. Размерность 768 жёстко завязана на nomic-embed-text.

## Единый фундамент анализа (Digest Engine)

```text
DigestService.GetOrBuildAsync(chatId):
  если выжимки есть в PeriodDigests → вернуть; иначе:
  режет весь чат по ~250 → по каждому куску AI-выжимка {summary,mood,events}
  (мусор отфильтрован, медиа посчитаны) → СОХРАНЯЕТ в PeriodDigests.
Поверх выжимок (один LLM-вызов каждый, полный охват):
  AiInsightService → инсайты;  LifeTimelineService → вехи;  RollupCache → итог+таймлайн.
Воркер строит выжимки с прогрессом job.Progress="N/M" перед агрегацией.
Refresh: insights/timeline — пересчёт поверх готовых выжимок; rollup — полная пересборка
(сброс PeriodDigests + Rollups + Insights + LifeTimelines).
```

## Полный анализ по периодам (Rollup Engine)

```text
POST /chats/{id}/rollup/async → фоном:
  фаза 1: сообщения чата режутся по ~250 → по каждому AI-выжимка
          {summary, mood, events[]} (мусор отфильтрован, медиа посчитаны);
          прогресс пишется в AiJob.Progress = "done/total";
  фаза 2: все выжимки → один AI-проход → {summary, timeline[]} → кэш Rollups.
GET /jobs/{id} возвращает progress для индикатора на фронте.
```
Видит 100% переписки (а не выборку 200), поэтому хронология полнее и точнее.

## Точки расширения / конфиг

- **AI в PDF целиком:** добавить хронологию и эволюцию в отчёт.
- **Группы (>2):** обобщить Relationship/баланс под N участников.
- **pgvector:** расширение Postgres, векторное поле в `Message`.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message`.
- Конфиг: `ConnectionStrings:Postgres` · `Ollama` (`llama3.1:8b` + `nomic-embed-text`) · `EmotionAnalysis` ·
  CORS→:5173 · HTTPS-редирект только вне Development · EF Npgsql, мажор=`net9.0`.
