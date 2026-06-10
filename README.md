# ChatInsight.Api — статус проекта

Backend для интеллектуального анализа переписок (пока — Telegram Export).
ASP.NET Core Web API на **.NET 9**, контроллеры + Swagger.

Этот документ — слепок текущего состояния: **что уже сделано**, **на чём остановились**, **что дальше**.
Идея и полное видение продукта — в `1.docx` (раздел *ChatInsight*).

---

## Что это сейчас

Рабочий API, который принимает выгрузку Telegram (`result.json`) и прогоняет её через набор аналитических сервисов: статистика, темы, эмоции, скорость ответа, инициатива, таймлайн, отношения и сводный отчёт. Всё считается **в памяти за один запрос** — без базы, без сохранения, без AI/LLM. Анализ построен на статистике и словарях (ключевые слова), не на ML.

---

## Структура проекта

```text
ChatInsight.Api/

├── Program.cs                  # точка входа, DI, Swagger, pipeline
├── appsettings.json
│
├── Properties/
│   └── launchSettings.json
│
├── Models/
│   ├── Telegram/
│   │   ├── TelegramExport.cs   # name, type, messages[]
│   │   └── TelegramMessage.cs  # id, type, date, from, text (object: string | массив entity)
│   └── Domain/                 # ⚠ ПУСТАЯ — задел под сущности БД (Chat, Message, ...)
│
├── Parsers/
│   └── TelegramParser.cs       # десериализация result.json → TelegramExport
│
├── Domain/
│   └── ChatAnalysisContext.cs  # ЕДИНЫЙ рантайм-контекст анализа: фильтр message+from,
│                               #   сортировка, участники, первая/последняя дата, total
│
├── Services/Text/
│   ├── TelegramTextExtractor.cs # достаёт чистый текст из text (строка ИЛИ массив entity)
│   └── TextCleaner.cs           # слова длиной ≥3, минус стоп-слова, минус chorus/verse
│
├── Services/Analytics/
│   ├── StatisticsService.cs     # объём, длина, по авторам/часам/дням, активный час
│   ├── TextAnalyticsService.cs  # топ-слова, всего символов/слов, слов на сообщение
│   ├── TopicService.cs          # топ-30 слов как «темы»
│   ├── EmotionService.cs        # позитив/негатив/мат по словарям, toxicity score
│   ├── ResponseService.cs       # среднее время ответа по авторам, кол-во ответов
│   ├── InitiativeService.cs     # кто начинает: день / после паузы (≥8ч)
│   ├── TimelineService.cs       # начало, пик, самая длинная пауза, всплески
│   ├── RelationshipService.cs   # баланс активности, доминирующий участник
│   └── ReportService.cs         # агрегатор: собирает всё в один отчёт
│
├── Controllers/                 # по контроллеру на каждый модуль (см. таблицу ниже)
│
├── DTOs/
│   └── ImportResultDto.cs       # ответ импорта (метаданные чата)
│
├── Analysis/                    # модели-результаты (DTO ответов)
│   ├── Statistics/ChatStatistics.cs
│   ├── Text/TextStatistics.cs
│   ├── Topics/{TopicItem, TopicStatistics}.cs
│   ├── Emotion/EmotionStatistics.cs
│   ├── Response/ResponseStatistics.cs
│   ├── Initiative/InitiativeStatistics.cs
│   ├── Timeline/TimelineEvent.cs
│   ├── Relationship/RelationshipReport.cs
│   └── Report/ReportStatistics.cs
│
├── Data/                        # ⚠ ПУСТАЯ — задел под DbContext / EF Core
└── Reports/                     # ⚠ ПУСТАЯ — задел под генерацию отчётов (PDF/HTML/MD)
```

**Архитектурная идея, которая уже выдержана:** парсер → `ChatAnalysisContext` (один раз фильтрует и сортирует сообщения) → сервисы считают свою метрику → контроллер отдаёт JSON. Сервисы не знают друг о друге, `ReportService` просто вызывает их по очереди.

> **Нюанс 1 — два источника данных.** Часть сервисов работает через `ChatAnalysisContext` (Statistics, Text, Topics, Emotion), а часть — напрямую через `TelegramExport` (Response, Initiative, Timeline, Relationship). Стоит привести к одному (контексту) — см. «Что дальше».
>
> **Нюанс 2 — две папки «Domain» (легко запутаться).** Есть `Domain/` (живой `ChatAnalysisContext`, рантайм-контекст анализа) и пустая `Models/Domain/` (задел под сущности БД). Это разные вещи с одинаковым словом в имени. План на будущее, чтобы не путаться:
> - EF-сущности БД (`Chat`, `Message`, `AnalysisResult`) → в `Models/Domain/` (или переименовать в `Entities/`);
> - `ChatAnalysisContext` — это **не** `DbContext`. Когда появится EF, рядом встанет `ChatInsightDbContext`, и два «...Context» будут сбивать с толку. Стоит переименовать рантайм-контекст в `AnalysisContext` / `ChatContext`, чтобы зарезервировать слово `DbContext` за базой.

---

## Endpoints

Все принимают `IFormFile file` (тот самый `result.json`), метод **POST**, `multipart/form-data`.

| Маршрут | Сервис | Что возвращает |
|---|---|---|
| `/api/import/telegram` | — (в контроллере) | метаданные: имя, тип, кол-во сообщений, первая/последняя дата, участники |
| `/api/analysis/basic` | StatisticsService | объём, средняя длина, по авторам/часам/дням, самый активный час |
| `/api/text` | TextAnalyticsService | топ-слова, всего символов/слов, слов на сообщение |
| `/api/topics` | TopicService | топ-30 слов как темы |
| `/api/emotion` | EmotionService | позитив/негатив/мат, toxicity score |
| `/api/response` | ResponseService | среднее время ответа и кол-во ответов по авторам |
| `/api/initiative` | InitiativeService | старты диалога: первый за день, после паузы ≥8ч |
| `/api/timeline` | TimelineService | события: начало, пик, длинная пауза, всплески |
| `/api/relationship` | RelationshipService | баланс активности, доминирующий участник |
| `/api/report` | ReportService | **всё сразу** в одном объекте |

---

## Что работает ✅

- [x] Проект ASP.NET Core Web API (.NET 9), Swagger, HTTPS
- [x] Импорт и парсинг Telegram Export (`result.json`)
- [x] `TelegramTextExtractor` — корректно вытаскивает текст, когда `text` это массив (ссылки, упоминания, форматирование)
- [x] `ChatAnalysisContext` — общий слой фильтрации/сортировки
- [x] Статистика активности (авторы / часы / дни / активный час / длина)
- [x] Текстовая аналитика и темы (частотность слов)
- [x] Эмоции по словарям (позитив / негатив / мат + toxicity)
- [x] Скорость ответа (с отсечкой пауз > 24ч)
- [x] Инициатива (кто начинает день / после паузы)
- [x] Таймлайн событий (начало, пик, пауза, всплески активности)
- [x] Базовый анализ отношений (баланс активности)
- [x] Сводный отчёт-агрегатор `/api/report`
- [x] DI: все сервисы зарегистрированы

---

## На чём остановились / чего ещё нет ⛔

- [ ] **База данных.** Нет EF Core, нет DbContext, нет PostgreSQL/pgvector. Папки `Data/` и `Models/Domain/` пустые. Каждый запрос заново парсит загруженный файл — ничего не сохраняется.
- [ ] **AI / LLM.** Ollama (Qwen/Gemma/Llama) не подключён. «Эмоции» и «темы» — это словари и частотность слов, а не модель. AI Insight Engine из идеи ещё не начат.
- [ ] **Генерация отчётов в файл.** `/api/report` отдаёт JSON. Экспорта в **PDF/HTML/Markdown** (как в MVP) пока нет. Папка `Reports/` пустая.
- [ ] **Frontend.** React + TS + Tailwind, графики — не начато.
- [ ] **Relationship** заполняет только `ActivityBalance` и `DominantParticipant`. Поля `InitiativeBalance` и `ResponseBalance` объявлены, но не считаются.
- [ ] **Только парные чаты по-настоящему осмысленны.** Relationship берёт топ-2 авторов; для групп логика не продумана.

---

## Что починить по мелочи (тех-долг) 🔧

- **`Program.cs`:** `StatisticsService` зарегистрирован дважды — убрать дубль.
- **Версия фреймворка:** в `csproj` стоит `net9.0`, в идее заявлен **.NET 10**. Определиться и выровнять.
- **Проверка файла:** только `ImportController` и `AnalysisController` проверяют `file == null/пустой`. Остальные контроллеры упадут на пустом запросе — вынести проверку в общий хелпер/фильтр.
- **CORS:** не настроен. Понадобится сразу, как появится React-фронт.
- **Единый источник данных:** перевести Response/Initiative/Timeline/Relationship на `ChatAnalysisContext`, чтобы фильтрация сообщений была в одном месте.
- **Коллизия имён `Domain` / `Context`:** две папки «Domain» (`Domain/` и пустая `Models/Domain/`) и будущий `DbContext` рядом с `ChatAnalysisContext`. До подключения базы решить: EF-сущности → `Models/Domain/` (или `Entities/`), рантайм-контекст переименовать в `AnalysisContext`/`ChatContext`. Подробности — в «Структуре проекта», Нюанс 2.
- **`ChatAnalysisContext.Create`** делает `messages.First()` — упадёт на чате без валидных сообщений. Добавить guard.
- **Словари эмоций/мата** маленькие и захардкожены в сервисе — вынести в конфиг/файл и расширить.

---

## Что дальше — порядок шагов

Ближайшее (доводим MVP из идеи):

1. **Database.** Подключить EF Core + Npgsql, создать `DbContext`, модели `Chat` / `Message` / `AnalysisResult`, миграции. Сохранять импорт, чтобы не парсить файл на каждый запрос.
2. **Persisted import.** `/api/import` сохраняет чат и возвращает `chatId`; остальные эндпоинты работают по `chatId`, а не по заново загруженному файлу.
3. **Экспорт отчёта в PDF** (пункт MVP). Затем HTML и Markdown.
4. **Починить тех-долг** из списка выше (дубль DI, проверки файла, CORS).

Среднее (AI и качество анализа):

5. **AI Engine на Ollama.** Эмоции/темы/инсайты через модель вместо словарей. Сводка-резюме переписки.
6. **Расширить Relationship** (инициатива + скорость ответа → индексы близости/вовлечённости).
7. **pgvector** для семантического поиска по сообщениям и тем.

Дальнее (из идеи, v2/v3):

8. **Life Timeline Engine** — жизненная хронология по переписке.
9. **Frontend** React + TS + Tailwind с графиками.
10. Поддержка Discord / WhatsApp / VK, групповой анализ, SaaS.

---

## Запуск

```bash
dotnet run
```

Swagger: `https://localhost:7015/swagger` (или `http://localhost:5201`).
Грузим `result.json` (Telegram → Экспорт истории чата → JSON) в любой POST-эндпоинт.

---

## Коротко

Есть крепкий аналитический backend: импорт Telegram + ~9 модулей статистики/текста/эмоций/таймлайна, собранные в общий контекст и сводный отчёт. Всё держится в памяти на один запрос. **Следующий настоящий рубеж — база данных и сохранение**, затем экспорт отчёта в PDF и подключение AI. Это превращает «считалку метрик» в продукт из `1.docx`.

```text
[✓] Web API + Swagger
[✓] Импорт + парсинг Telegram
[✓] ChatAnalysisContext
[✓] Statistics / Text / Topics / Emotion
[✓] Response / Initiative / Timeline / Relationship
[✓] Сводный отчёт /api/report

[ ] База данных (PostgreSQL + EF Core + pgvector)
[ ] Сохранение импорта (работа по chatId)
[ ] Экспорт отчёта в PDF / HTML / Markdown
[ ] AI Engine (Ollama)
[ ] Frontend (React + TS + Tailwind)
[ ] Life Timeline Engine
```