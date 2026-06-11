# ChatInsight — ARCHITECTURE

Слои и потоки данных. Статус — `PROJECT_STATUS.md`, план — `ROADMAP.md`.

```text
chatinsight-web (React/Vite :5173)
        │  REST + CORS
        ▼
ChatInsight.Api (ASP.NET Core :5201)
        ├─ PostgreSQL (EF Core)
        └─ Ollama (:11434, локальная LLM)
```

---

## Потоки данных

### Импорт (файл → БД, дозагрузка)
```text
POST /api/import/telegram → ReadExportAsync → TelegramParser → TelegramExport
   → ChatImportService: по SourceId есть → добавить новые (по TelegramId), сбросить кэш AI
                        нет → создать чат
   → PostgreSQL → {chatId, isNewChat, newMessages}
```

### Анализ (БД → отчёт/PDF/сравнение)
```text
GET /api/chats/{id}/report(.pdf)|/compare
   → ChatContextLoader → ChatAnalysisContext.Create (фильтр+сортировка, 1 раз)
   → ReportService (вкл. RelationshipService) / ComparisonService / PdfReportService
```

### AI (БД → Ollama → кэш)
```text
GET /api/chats/{id}/insights|/personality
   → *CacheService: есть и не refresh → отдать (X-Insight-Cache: hit)
                    иначе → *Service (Ollama, JSON Schema) → сохранить → отдать
   → недоступна → 503
PDF ?ai=true → берёт insight + portraits из кэша/GetOrCreate и встраивает в документ
```

Принцип: фильтрация/сортировка один раз в `ChatAnalysisContext.Create`; сервисы не
знают, откуда данные.

---

## Слои (backend)

| Папка | Что |
|---|---|
| `Models/Telegram/` | `TelegramExport`(+Id), `TelegramMessage` |
| `Models/Domain/` | `Chat`(+SourceId), `Message`, `ChatInsightRecord`, `PersonalityRecord` |
| `Parsers/` · `Data/` · `Domain/` | парсер · `DbContext`+миграции · `ChatAnalysisContext` |
| `Services/Text/` · `Services/Import/` | текст · импорт (upsert) |
| `Services/Analytics/` | `*Service`, `ReportService`, `RelationshipService`, `ComparisonService`, `ChatContextLoader` |
| `Services/Ai/` | `OllamaClient`, `AiInsightService(+Cache)`, `PersonalityService(+Cache)` |
| `Reports/` | `PdfReportService` (AI-анализ + портреты опционально) |
| `Analysis/<Модуль>/` · `Controllers/` · `Configuration/` | DTO · HTTP · опции |

`RelationshipService` инжектит `InitiativeService`+`ResponseService` и считает
балансы активности/инициативы/ответов; результат — в `ReportStatistics.Relationship`.

---

## Модель БД

```text
Chat 1─< Message ; Chat 1─1 ChatInsightRecord ; Chat 1─< PersonalityRecord (uniq ChatId+Participant)

Chat:    Id, SourceId, Name, Type, ImportedAt, UpdatedAt?, MessageCount
Message: Id, ChatId, TelegramId, Type, Date(ts без tz), Author?, Text, RawTextJson?
Insights/Personalities: кэш AI (списки → text[] Npgsql)
```

---

## Frontend (chatinsight-web)

```text
src/api.ts · types.ts
src/components/  Layout.tsx · Charts.tsx (Recharts: часы, авторы)
src/pages/       Upload.tsx · ChatList.tsx · ChatDetail.tsx (метрики, графики,
                 баланс отношений, AI-блоки по кнопке, сравнение, PDF)
```
Spectral (display) + Inter + JetBrains Mono; индиго-акцент. AI-блоки — по кнопке.

---

## Точки расширения

- **Async AI:** `BackgroundService`/очередь, статус задачи, опрос с фронта.
- **pgvector:** расширение Postgres, векторное поле в `Message`.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message`.

## Конфигурация
`ConnectionStrings:Postgres` · `Ollama` (модель `llama3.1:8b`) · `EmotionAnalysis` ·
CORS `frontend`→:5173 · HTTPS-редирект только вне Development · EF: Npgsql+Design, мажор=`net9.0`.
