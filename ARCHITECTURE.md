# ChatInsight.Api — ARCHITECTURE

Как устроен backend и куда что класть. Статус — в `PROJECT_STATUS.md`,
план — в `ROADMAP.md`.

---

## Поток данных

```text
HTTP POST (result.json)
        │
        ▼
Controller : AnalysisControllerBase
        │   ReadExportAsync(file)  ── валидация: null/пусто, битый JSON, нет сообщений
        ▼
TelegramParser ──────────────► TelegramExport   (сырой десериализованный JSON)
        │
        ▼
ChatAnalysisContext.Create(export)
        │   • фильтр: type == "message" && from != null
        │   • сортировка по дате
        │   • участники, первая/последняя дата, total, IsEmpty
        ▼
ChatAnalysisContext  ──────────► *Service.Analyze(context)
        │                          (Statistics, Text, Topics, Emotion,
        │                           Response, Initiative, Timeline,
        │                           Relationship)
        ▼
*Statistics / *Report (модели-результаты)  ──►  JSON ответ
```

Ключевая идея: **сообщения фильтруются и сортируются ровно один раз** — в
`ChatAnalysisContext.Create`. Сервисы не трогают сырой `TelegramExport` и не знают
друг о друге. `ReportService` просто вызывает остальные по очереди и собирает
результат.

---

## Слои и папки

| Папка | Слой | Что лежит |
|---|---|---|
| `Models/Telegram/` | Контракт ввода | `TelegramExport`, `TelegramMessage` — форма Telegram JSON |
| `Parsers/` | Парсинг | `TelegramParser` — десериализация потока |
| `Domain/` | Рантайм-контекст | `ChatAnalysisContext` — подготовленные данные для анализа |
| `Services/Text/` | Утилиты текста | `TelegramTextExtractor`, `TextCleaner` |
| `Services/Analytics/` | Аналитика | `*Service` — по одному на метрику + `ReportService`-агрегатор |
| `Analysis/<Модуль>/` | Результаты | `*Statistics`, `TimelineEvent`, `TopicItem` и т.д. (DTO ответов) |
| `Controllers/` | HTTP | по контроллеру на модуль + `AnalysisControllerBase` |
| `Configuration/` | Настройки | `EmotionAnalysisOptions` (биндинг секций appsettings) |
| `DTOs/` | DTO | `ImportResultDto` |
| `Data/` | **(задел)** БД | будущий `ChatInsightDbContext`, EF |
| `Models/Domain/` | **(задел)** Сущности | будущие EF-сущности `Chat`/`Message`/... |
| `Reports/` | **(задел)** Экспорт | сгенерированные PDF/HTML/MD |

---

## Соглашения по папкам и именам (важно — чтобы не путаться)

В проекте два «контекста» и два «Domain». Чтобы при добавлении БД ничего не
смешалось, фиксируем договорённость:

- **`ChatAnalysisContext`** (папка `Domain/`, namespace `...Domain`) — это
  **рантайм-контекст анализа**, НЕ база. Имя оставляем как есть.
- **`ChatInsightDbContext`** (будущий, папка `Data/`) — это EF Core `DbContext`.
  Слово «DbContext» резервируем только за ним.
- **EF-сущности** (`Chat`, `Message`, `Participant`, `AnalysisResult`) кладём в
  **`Models/Domain/`** (namespace `...Models.Domain`). Если со временем покажется
  путаным — переименовать папку в `Entities/`, но НЕ смешивать с `Domain/`.
- `Analysis/*` — только модели-ответы (то, что уходит в JSON), без логики.

Итог: `Domain/` = «как анализируем сейчас», `Models/Domain/` = «что храним в БД».
Разные вещи, специально разведены.

---

## Как добавить новый аналитический модуль

1. Модель результата → `Analysis/<Модуль>/<Имя>Statistics.cs`.
2. Сервис → `Services/Analytics/<Имя>Service.cs` с
   `public <Имя>Statistics Analyze(ChatAnalysisContext context)`.
   Работать **только** через `context.Messages` (уже отфильтровано/отсортировано).
3. Регистрация в `Program.cs`: `builder.Services.AddScoped<<Имя>Service>();`.
4. Контроллер → `Controllers/<Имя>Controller.cs : AnalysisControllerBase`,
   паттерн как у остальных (`ReadExportAsync` → `Create` → `Analyze`).
5. (Опц.) добавить в `ReportService`, если нужно в сводный отчёт.

---

## Точки расширения под будущее

- **БД (Этап 1):** `ChatAnalysisContext.Create` начнёт получать сообщения из БД по
  `chatId`, а не из загруженного файла. Сервисы менять не нужно — они уже на контексте.
- **AI (Этап 2):** новый `Services/Ai/` + клиент Ollama. `EmotionService`/`TopicService`
  получают AI-вариант реализации; контракт `Analyze(context)` сохраняется.
- **Новые источники:** новый парсер (Discord/WhatsApp/VK) → маппинг в
  `TelegramMessage`-подобную модель или общий `Message` → тот же `ChatAnalysisContext`.

---

## Конфигурация

- Словари эмоций — `appsettings.json` → секция `EmotionAnalysis`
  (биндится в `EmotionAnalysisOptions` через `IOptions<>`).
- CORS-политика `frontend` — в `Program.cs` (`localhost:5173`, `localhost:3000`).
- Секреты (будущая строка подключения к PostgreSQL) — **только User Secrets**,
  не в `appsettings.json` (`UserSecretsId` уже прописан в `.csproj`).
