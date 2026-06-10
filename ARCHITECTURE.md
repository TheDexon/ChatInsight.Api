# ChatInsight.Api — ARCHITECTURE

Как устроен backend и куда что класть. Статус — в `PROJECT_STATUS.md`,
план — в `ROADMAP.md`.

---

## Два пути данных

### 1. Импорт (файл → БД)

```text
POST /api/import/telegram (result.json)
        │
        ▼
AnalysisControllerBase.ReadExportAsync ── валидация: null/пусто, битый JSON, нет сообщений
        ▼
TelegramParser ─────────► TelegramExport (сырой JSON)
        ▼
ChatImportService
        │   • маппит сообщения в сущности Message
        │   • Text = плоский текст (TelegramTextExtractor)
        │   • RawTextJson = оригинальный text (на будущее)
        ▼
ChatInsightDbContext.SaveChanges ──► PostgreSQL (Chats, Messages)
        ▼
ответ: chatId + метаданные
```

### 2. Анализ (БД → отчёт)

```text
GET /api/chats/{id}/report
        │
        ▼
ChatContextLoader.LoadAsync(chatId)
        │   • грузит Chat + Messages из БД
        │   • маппит обратно в TelegramExport
        ▼
ChatAnalysisContext.Create(export)
        │   • фильтр type=="message" && from!=null
        │   • сортировка по дате, участники, даты, total, IsEmpty
        ▼
ReportService.Analyze(context) ──► *Service.Analyze(context) ──► JSON
```

Ключевая идея сохранена: сообщения фильтруются/сортируются ровно один раз — в
`ChatAnalysisContext.Create`. Сервисы аналитики **не знают**, откуда пришли данные
(файл или БД) — они всегда работают с `ChatAnalysisContext`. Поэтому переход на БД
не потребовал менять ни один аналитический сервис.

> File-эндпоинты (`/api/emotion`, `/api/timeline`, ...) строят `ChatAnalysisContext`
> прямо из загруженного файла — тот же контекст, только без сохранения. Удобно для
> разовых прогонов; в перспективе переводятся на `chatId`.

---

## Слои и папки

| Папка | Слой | Что лежит |
|---|---|---|
| `Models/Telegram/` | Контракт ввода | `TelegramExport`, `TelegramMessage` — форма Telegram JSON |
| `Models/Domain/` | **Сущности БД** | `Chat`, `Message` (EF Core) |
| `Parsers/` | Парсинг | `TelegramParser` |
| `Data/` | **Доступ к БД** | `ChatInsightDbContext`, миграции (`Migrations/`) |
| `Domain/` | Рантайм-контекст | `ChatAnalysisContext` — подготовленные данные для анализа |
| `Services/Text/` | Утилиты текста | `TelegramTextExtractor`, `TextCleaner` |
| `Services/Import/` | Импорт | `ChatImportService` — парсинг → сущности → БД |
| `Services/Analytics/` | Аналитика | `*Service` + `ReportService` + `ChatContextLoader` (БД → контекст) |
| `Analysis/<Модуль>/` | Результаты | `*Statistics`, `TimelineEvent`, `TopicItem` (DTO ответов) |
| `Controllers/` | HTTP | контроллеры модулей + `ChatsController` (БД) + `AnalysisControllerBase` |
| `Configuration/` | Настройки | `EmotionAnalysisOptions` |
| `DTOs/` | DTO | `ImportResultDto` (теперь с `ChatId`) |
| `Reports/` | **(задел)** Экспорт | будущие PDF/HTML/MD |

---

## Соглашения по именам (важно — чтобы не путаться)

В проекте два «контекста». Договорённость соблюдена:

- **`ChatAnalysisContext`** (`Domain/`) — рантайм-контекст анализа, НЕ база.
- **`ChatInsightDbContext`** (`Data/`) — EF Core `DbContext`, работа с PostgreSQL.

Имена разные, не конфликтуют. EF-сущности (`Chat`, `Message`) — в `Models/Domain/`.

---

## Модель БД

```text
Chat 1 ───< Many Message      (FK Message.ChatId, ON DELETE CASCADE)

Chat:    Id(Guid PK), Name, Type, ImportedAt(utc), MessageCount
Message: Id(long PK), ChatId(FK), TelegramId, Type, Date(timestamp без tz),
         Author?, Text, RawTextJson?
Индексы: Message(ChatId), Message(ChatId, Date)
```

Почему `Date` = `timestamp without time zone`: Telegram отдаёт время без таймзоны;
так сохраняем «как в файле», без сдвигов в анализе по часам. `ImportedAt` — UTC.

Почему хранится и `Text`, и `RawTextJson`: плоский `Text` — для быстрой аналитики;
`RawTextJson` (оригинальный `text`) — чтобы потом достать ссылки/форматирование или
переобработать AI без переимпорта.

---

## Как добавить новый аналитический модуль

1. Модель результата → `Analysis/<Модуль>/<Имя>Statistics.cs`.
2. Сервис → `Services/Analytics/<Имя>Service.cs` с
   `Analyze(ChatAnalysisContext context)`. Работать только через `context.Messages`.
3. DI в `Program.cs`: `AddScoped<<Имя>Service>()`.
4. Контроллер: по файлу — через `AnalysisControllerBase`; по БД — через `ChatContextLoader`.
5. (Опц.) добавить в `ReportService`.

---

## Точки расширения под будущее

- **AI (Ollama):** новый `Services/Ai/` + HTTP-клиент. `EmotionService`/`TopicService`
  получают AI-вариант; контракт `Analyze(context)` сохраняется.
- **Экспорт в PDF:** новый сервис в `Reports/`, берёт `ReportStatistics` и рендерит файл.
- **pgvector:** включить расширение в Postgres, добавить векторное поле в `Message`,
  EF-маппинг через Npgsql.Pgvector.
- **Новые источники (Discord/WhatsApp/VK):** новый парсер → маппинг в `Message`/`TelegramExport` → тот же конвейер.

---

## Конфигурация

- Строка подключения — `ConnectionStrings:Postgres` (`appsettings.json`),
  локальный Postgres из `docker-compose.yml`. Прод — env `ConnectionStrings__Postgres`.
- Словари эмоций — секция `EmotionAnalysis` (`IOptions<EmotionAnalysisOptions>`).
- CORS-политика `frontend` — в `Program.cs`.
- Стек EF: Npgsql provider + EF Core Design, мажор = таргету (сейчас 9.x под net9.0).