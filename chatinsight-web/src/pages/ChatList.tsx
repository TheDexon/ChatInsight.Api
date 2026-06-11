import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { listChats } from "../api";
import type { ChatListItem } from "../types";

export default function ChatList() {
  const [chats, setChats] = useState<ChatListItem[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    listChats().then(setChats).catch(() => setError(true));
  }, []);

  if (error)
    return (
      <Empty
        title="Бэкенд не отвечает"
        body="Запусти API (dotnet run) и Postgres (docker compose up -d), потом обнови страницу."
      />
    );

  if (!chats) return <div className="text-muted">Загружаю…</div>;

  if (chats.length === 0)
    return (
      <Empty
        title="Пока пусто"
        body="Загрузи первый экспорт Telegram, чтобы начать анализ."
        cta
      />
    );

  return (
    <div>
      <h1 className="font-display text-3xl font-semibold mb-8">Чаты</h1>
      <div className="space-y-3">
        {chats.map((c) => (
          <Link
            key={c.id}
            to={`/chat/${c.id}`}
            className="block rounded-xl border border-line bg-card p-5 hover:border-accent transition-colors"
          >
            <div className="flex items-center justify-between">
              <div className="font-display text-lg">{c.name || "Без названия"}</div>
              <div className="font-mono text-sm text-muted">{c.messageCount} сообщ.</div>
            </div>
            <div className="font-mono text-xs text-muted mt-1">
              {c.type} · загружен {new Date(c.importedAt).toLocaleDateString("ru-RU")}
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}

function Empty({ title, body, cta }: { title: string; body: string; cta?: boolean }) {
  return (
    <div className="rounded-xl border border-line bg-card p-10 text-center">
      <div className="font-display text-2xl mb-2">{title}</div>
      <p className="text-muted mb-5">{body}</p>
      {cta && (
        <Link to="/upload" className="bg-ink text-paper px-4 py-2 rounded-md text-sm hover:bg-accent transition-colors">
          Загрузить переписку
        </Link>
      )}
    </div>
  );
}
