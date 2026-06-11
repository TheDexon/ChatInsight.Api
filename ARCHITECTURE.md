# ChatInsight.Api — ARCHITECTURE

Как устроен backend и куда что класть. Статус — в `PROJECT_STATUS.md`,
план — в `ROADMAP.md`.

---

## Пути данных

### 1. Импорт (файл → БД)

```text
POST /api/import/telegram (result.json)
   → AnalysisControllerBase.ReadExportAsync (валидация)
   → TelegramParser → TelegramExport
   → ChatImportService (Text = плоский, RawTextJson = оригинал)
   → ChatInsightDbContext.SaveChanges → PostgreSQL (Chats, Messages)
   → ответ: chatId + метаданные
```

### 2. Анализ из БД (статистика / PDF)

```text
GET /api/chats/{id}/report(.pdf)
   → ChatContextLoader.LoadAsync(chatId)   (Messages → TelegramExport)
   → ChatAnalysisContext.Create            (фильтр+сортировка, 1 раз)
   → ReportService.Analyze(context) → JSON
   → (PDF) PdfReportService.Build(context) → QuestPDF → файл
```

### 3. AI-инсайты (БД → Ollama)

```text
GET /api/chats/{id}/insights
   → ChatContextLoader.LoadAsync(chatId)
   → AiInsightService: выборка сообщений + промпт + JSON Schema
   → OllamaClient → POST localhost:11434/api/chat (format = schema)
   → парсинг в AiInsight (summary, emotionalTone, topics[], dynamics[])
   → Ollama недоступна → OllamaUnavailableException → 503
```

Главный принцип сохранён: сообщения фильтруются/сортируются один раз в
`ChatAnalysisContext.Create`. Сервисы (аналитика, PDF, AI) не знают, откуда
данные — всегда работают с `ChatAnalysisContext`.

---

## Слои и папки

| Папка | Слой | Что лежит |
|---|---|---|
| `Models/Telegram/` | Контракт ввода | `TelegramExport`, `TelegramMessage` |
| `Models/Domain/` | Сущности БД | `Chat`, `Message` (EF Core) |
| `Parsers/` | Парсинг | `TelegramParser` |
| `Data/` | Доступ к БД | `ChatInsightDbContext`, `Migrations/` |
| `Domain/` | Рантайм-контекст | `ChatAnalysisContext` |
| `Services/Text/` | Утилиты текста | `TelegramTextExtractor`, `TextCleaner` |
| `Services/Import/` | Импорт | `ChatImportService` |
| `Services/Analytics/` | Аналитика | `*Service`, `ReportService`, `ChatContextLoader` |
| `Services/Ai/` | AI | `OllamaClient`, `AiInsightService` |
| `Reports/` | Экспорт | `PdfReportService` (QuestPDF) |
| `Analysis/<Модуль>/` | Результаты | `*Statistics`, `AiInsight`, … (DTO ответов) |
| `Controllers/` | HTTP | модули + `ChatsController`, `AiController`, `ImportController`, `AnalysisControllerBase` |
| `Configuration/` | Настройки | `EmotionAnalysisOptions`, `OllamaOptions` |
| `DTOs/` | DTO | `ImportResultDto` |

---

## Соглашения по именам

- **`ChatAnalysisContext`** (`Domain/`) — рантайм-контекст анализа, НЕ база.
- **`ChatInsightDbContext`** (`Data/`) — EF Core `DbContext` (PostgreSQL).
- EF-сущности (`Chat`, `Message`) — в `Models/Domain/`.

---

## Модель БД

```text
Chat 1 ───< Many Message    (FK Message.ChatId, ON DELETE CASCADE)

Chat:    Id(Guid PK), Name, Type, ImportedAt(utc), MessageCount
Message: Id(long PK), ChatId(FK), TelegramId, Type, Date(timestamp без tz),
         Author?, Text, RawTextJson?
Индексы: Message(ChatId), Message(ChatId, Date)
```

`Date` = `timestamp without time zone`: Telegram отдаёт время без таймзоны,
храним «как в файле», без сдвигов. Хранятся и `Text` (для анализа), и
`RawTextJson` (оригинал — под ссылки/форматирование/переобработку AI).

---

## AI-слой (детали)

- **Конфиг** `OllamaOptions` (секция `Ollama`): `BaseUrl`, `Model`, `TimeoutSeconds`,
  `SampleMessages`. Модель меняется без пересборки.
- **`OllamaClient`** — через `IHttpClientFactory`, увеличенный таймаут (LLM медленный).
  Если передан `schema` — кладётся в `format`, движок гарантирует структуру ответа.
- **`AiInsightService`** — строит JSON Schema (`required` все поля), выборку сообщений,
  промпт; парсит ответ в `AiInsight`. На не-JSON — грейсфул-фолбэк в `summary`.
- **Выбор модели**: `llama3.1:8b` (русский, без морализаторства, стабильный язык).
  `qwen2.5:14b` в structured-режиме сваливалась в китайский — задокументировано как
  непригодная для этой задачи.

---

## Как добавить аналитический модуль

1. Результат → `Analysis/<Модуль>/<Имя>Statistics.cs`.
2. Сервис → `Services/Analytics/<Имя>Service.cs`, `Analyze(ChatAnalysisContext)`.
3. DI в `Program.cs`.
4. Эндпоинт: по файлу — `AnalysisControllerBase`; по БД — `ChatContextLoader`.
5. (Опц.) добавить в `ReportService` / в PDF.

---

## Точки расширения

- **Кэш AI:** таблица `AiInsight` (FK на `Chat`), считать один раз, отдавать из БД.
- **Async AI:** очередь/`BackgroundService`, статус задачи.
- **AI в PDF:** `PdfReportService` принимает `AiInsight` и рисует секцию.
- **pgvector:** включить расширение, векторное поле в `Message`, Npgsql.Pgvector.
- **Новые источники:** парсер Discord/WhatsApp/VK → та же модель `Message` → конвейер.

---

## Конфигурация

- `ConnectionStrings:Postgres` — БД (локально из `docker-compose.yml`; прод — env).
- `Ollama` — адрес/модель/таймаут/размер выборки.
- `EmotionAnalysis` — словари эмоций (`IOptions`).
- CORS-политика `frontend` — в `Program.cs`.
- Стек EF: Npgsql + EF Core Design, мажор = таргету (`9.x` под `net9.0`).
