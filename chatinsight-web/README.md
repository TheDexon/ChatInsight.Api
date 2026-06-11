# ChatInsight — фронтенд

React + TypeScript + Vite + Tailwind. Говорит с ChatInsight.Api.

## Запуск

```bash
npm install
npm run dev
```

Откроется на `http://localhost:5173`.
Бэкенд должен быть запущен на `http://localhost:5201` (CORS уже настроен под этот порт).
Если адрес API другой — поменяй `BASE_URL` в `src/api.ts`.

## Страницы

- `/` — список сохранённых чатов
- `/upload` — загрузка `result.json` (Telegram Export)
- `/chat/:id` — анализ: метрики, графики, AI-анализ, портреты участников, сравнение периодов

## Сборка

```bash
npm run build
```
