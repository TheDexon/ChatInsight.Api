import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import {
  getReport, getInsights, getPersonality, getComparison, reportPdfUrl,
} from "../api";
import type { Report, AiInsight, PersonalityProfile, PeriodComparison } from "../types";
import { HourChart, AuthorChart } from "../components/Charts";

export default function ChatDetail() {
  const { id = "" } = useParams();
  const [report, setReport] = useState<Report | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    getReport(id).then(setReport).catch(() => setError(true));
  }, [id]);

  if (error) return <p className="text-muted">Не удалось загрузить отчёт.</p>;
  if (!report) return <p className="text-muted">Считаю отчёт…</p>;

  const s = report.statistics;
  const e = report.emotion;

  return (
    <div className="space-y-10">
      <header className="flex items-end justify-between flex-wrap gap-4">
        <div>
          <div className="font-mono text-xs text-muted mb-1">отчёт по переписке</div>
          <h1 className="font-display text-3xl font-semibold">Анализ</h1>
        </div>
        <div className="flex gap-2">
          <a href={reportPdfUrl(id, true)} target="_blank" rel="noreferrer"
            className="bg-ink text-paper px-4 py-2 rounded-md text-sm hover:bg-accent transition-colors">
            Скачать PDF
          </a>
        </div>
      </header>

      <section className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Stat label="сообщений" value={s.totalMessages} />
        <Stat label="ср. длина" value={`${Math.round(s.averageMessageLength)}`} />
        <Stat label="активный час" value={`${s.mostActiveHour}:00`} />
        <Stat label="токсичность" value={`${e.toxicityScore}%`} accent={e.toxicityScore > 5} />
      </section>

      <Block title="Активность по часам">
        <HourChart byHour={s.messagesByHour} />
      </Block>

      <Block title="Сообщений по авторам">
        <AuthorChart byAuthor={s.messagesByAuthor} />
      </Block>

      <AiInsightBlock id={id} />
      <PersonalityBlock id={id} />
      <CompareBlock id={id} />
    </div>
  );
}

function Stat({ label, value, accent }: { label: string; value: string | number; accent?: boolean }) {
  return (
    <div className="rounded-xl border border-line bg-card p-4">
      <div className={`font-mono text-2xl ${accent ? "text-ember" : "text-ink"}`}>{value}</div>
      <div className="text-xs text-muted mt-1">{label}</div>
    </div>
  );
}

function Block({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section>
      <h2 className="font-display text-xl mb-4">{title}</h2>
      <div className="rounded-xl border border-line bg-card p-5">{children}</div>
    </section>
  );
}

function AiInsightBlock({ id }: { id: string }) {
  const [data, setData] = useState<AiInsight | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getInsights(id)); }
    catch { setErr("Не удалось получить AI-анализ. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">AI-анализ</h2>
        {!data && (
          <button onClick={run} disabled={busy}
            className="text-sm border border-line rounded-md px-3 py-1.5 hover:border-accent transition-colors">
            {busy ? "Модель думает…" : "Запустить"}
          </button>
        )}
      </div>
      {err && <div className="rounded-lg bg-ember/10 text-ember px-4 py-3 text-sm">{err}</div>}
      {data && (
        <div className="rounded-xl border border-line bg-accent-soft/50 p-5 space-y-3">
          <p>{data.summary}</p>
          <p className="text-sm"><b>Эмоциональный фон.</b> {data.emotionalTone}</p>
          {data.topics.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {data.topics.map((t) => (
                <span key={t} className="font-mono text-xs bg-card border border-line rounded px-2 py-1">{t}</span>
              ))}
            </div>
          )}
          {data.dynamics.length > 0 && (
            <ul className="text-sm list-disc pl-5 space-y-1">
              {data.dynamics.map((d, i) => <li key={i}>{d}</li>)}
            </ul>
          )}
          <div className="font-mono text-[11px] text-muted">{data.model}</div>
        </div>
      )}
    </section>
  );
}

function PersonalityBlock({ id }: { id: string }) {
  const [data, setData] = useState<PersonalityProfile[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getPersonality(id)); }
    catch { setErr("Не удалось построить портреты. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">Портреты участников</h2>
        {!data && (
          <button onClick={run} disabled={busy}
            className="text-sm border border-line rounded-md px-3 py-1.5 hover:border-accent transition-colors">
            {busy ? "Модель думает…" : "Запустить"}
          </button>
        )}
      </div>
      {err && <div className="rounded-lg bg-ember/10 text-ember px-4 py-3 text-sm">{err}</div>}
      {data && (
        <div className="grid md:grid-cols-2 gap-3">
          {data.map((p) => (
            <div key={p.participant} className="rounded-xl border border-line bg-card p-5">
              <div className="font-display text-lg mb-1">{p.participant}</div>
              <p className="text-sm mb-3">{p.summary}</p>
              <p className="text-sm text-muted mb-3"><b className="text-ink">Стиль.</b> {p.communicationStyle}</p>
              <div className="flex flex-wrap gap-2">
                {p.traits.map((t) => (
                  <span key={t} className="font-mono text-xs bg-accent-soft rounded px-2 py-1">{t}</span>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function CompareBlock({ id }: { id: string }) {
  const [data, setData] = useState<PeriodComparison | null>(null);
  const [busy, setBusy] = useState(false);

  async function run() {
    setBusy(true);
    try { setData(await getComparison(id)); } finally { setBusy(false); }
  }

  const delta = (n: number, unit: string) => {
    const sign = n > 0 ? "+" : "";
    const color = n > 0 ? "text-ember" : n < 0 ? "text-sage" : "text-muted";
    return <span className={`font-mono ${color}`}>{sign}{n}{unit}</span>;
  };

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">Было → стало</h2>
        {!data && (
          <button onClick={run} disabled={busy}
            className="text-sm border border-line rounded-md px-3 py-1.5 hover:border-accent transition-colors">
            {busy ? "Считаю…" : "Сравнить периоды"}
          </button>
        )}
      </div>
      {data && (
        <div className="rounded-xl border border-line bg-card p-5 space-y-4">
          <p className="text-sm">{data.summary}</p>
          <div className="grid grid-cols-3 gap-3 text-sm">
            <div>сообщения {delta(data.messagesDelta, "")}</div>
            <div>токсичность {delta(data.toxicityDelta, "%")}</div>
            <div>скорость ответа {delta(data.responseMinutesDelta, "м")}</div>
          </div>
          {data.newTopics.length > 0 && (
            <div className="text-sm"><b>Новые темы:</b> {data.newTopics.join(", ")}</div>
          )}
          {data.fadedTopics.length > 0 && (
            <div className="text-sm text-muted"><b>Ушли:</b> {data.fadedTopics.join(", ")}</div>
          )}
        </div>
      )}
    </section>
  );
}
