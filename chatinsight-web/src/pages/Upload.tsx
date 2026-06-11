import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { importTelegram } from "../api";
import type { ImportResult } from "../types";

export default function Upload() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const navigate = useNavigate();

  async function onFile(file: File) {
    setBusy(true);
    setError(null);
    try {
      const res = await importTelegram(file);
      setResult(res);
    } catch {
      setError("Не удалось загрузить файл. Проверь, что это result.json от Telegram и бэкенд запущен.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="max-w-xl">
      <h1 className="font-display text-3xl font-semibold mb-2">Загрузить переписку</h1>
      <p className="text-muted mb-8">
        Экспорт из Telegram в формате JSON. Настройки → Экспорт истории чата → формат JSON.
      </p>

      <label
        className={`block rounded-xl border-2 border-dashed p-10 text-center cursor-pointer transition-colors ${
          busy ? "border-line opacity-60" : "border-line hover:border-accent"
        }`}
      >
        <input
          type="file"
          accept="application/json,.json"
          className="hidden"
          disabled={busy}
          onChange={(e) => e.target.files?.[0] && onFile(e.target.files[0])}
        />
        <div className="font-display text-lg mb-1">
          {busy ? "Читаю файл…" : "Перетащи или выбери result.json"}
        </div>
        <div className="text-sm text-muted font-mono">json</div>
      </label>

      {error && (
        <div className="mt-4 rounded-lg bg-ember/10 text-ember px-4 py-3 text-sm">{error}</div>
      )}

      {result && (
        <div className="mt-6 rounded-xl border border-line bg-card p-5">
          <div className="font-display text-xl mb-1">{result.chatName}</div>
          <div className="text-sm text-muted mb-4 font-mono">
            {result.isNewChat ? "новый чат" : "дополнен"} · +{result.newMessages} сообщений ·
            всего {result.messagesCount}
          </div>
          <button
            onClick={() => navigate(`/chat/${result.chatId}`)}
            className="bg-ink text-paper px-4 py-2 rounded-md text-sm hover:bg-accent transition-colors"
          >
            Открыть анализ
          </button>
        </div>
      )}
    </div>
  );
}
