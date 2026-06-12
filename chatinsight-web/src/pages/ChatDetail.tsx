import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import {
  getReport, getComparison, reportPdfUrl,
  getInsightsAsync, getPersonalityAsync, getLifeTimelineAsync, getEvolutionAsync,
} from "../api";
import type {
  Report, AiInsight, PersonalityProfile, PeriodComparison, Relationship,
  LifeTimelineResult, PersonalityEvolutionResult,
} from "../types";
import { HourChart, AuthorChart, DayChart } from "../components/Charts";

function statusLabel(s: string): string {
  if (s === "pending") return "В очереди…";
  if (s === "running") return "Модель думает…";
  return "Считаю…";
}

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
        <a href={reportPdfUrl(id, true)} target="_blank" rel="noreferrer"
          className="bg-ink text-paper px-4 py-2 rounded-md text-sm hover:bg-accent transition-colors">
          Скачать PDF
        </a>
      </header>

      <section className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Stat label="сообщений" value={s.totalMessages} />
        <Stat label="ср. длина" value={`${Math.round(s.averageMessageLength)}`} />
        <Stat label="активный час" value={`${s.mostActiveHour}:00`} />
        <Stat label="токсичность" value={`${e.toxicityScore}%`} accent={e.toxicityScore > 5} />
      </section>

      {report.relationship && report.relationship.dominantParticipant && (
        <RelationshipBlock rel={report.relationship} />
      )}

      <Block title="Активность по дням"><DayChart byDay={s.messagesByDay} /></Block>
      <Block title="Эмоциональный фон"><EmotionBars e={e} total={s.totalMessages} /></Block>
      <Block title="Активность по часам"><HourChart byHour={s.messagesByHour} /></Block>
      <Block title="Сообщений по авторам"><AuthorChart byAuthor={s.messagesByAuthor} /></Block>

      <AiInsightBlock id={id} />
      <PersonalityBlock id={id} />
      <LifeTimelineBlock id={id} />
      <EvolutionBlock id={id} />
      <CompareBlock id={id} />
    </div>
  );
}

function EmotionBars({ e, total }: { e: Report["emotion"]; total: number }) {
  const rows = [
    { label: "Позитивные", value: e.positiveMessages, color: "bg-sage" },
    { label: "Негативные", value: e.negativeMessages, color: "bg-ember" },
    { label: "С матом", value: e.profanityMessages, color: "bg-accent" },
  ];
  const max = Math.max(1, ...rows.map((r) => r.value));
  return (
    <div className="space-y-3">
      {rows.map((r) => (
        <div key={r.label}>
          <div className="flex justify-between text-xs mb-1">
            <span>{r.label}</span>
            <span className="font-mono text-muted">
              {r.value}{total > 0 && ` · ${Math.round((r.value / total) * 100)}%`}
            </span>
          </div>
          <div className="h-2 rounded-full bg-line overflow-hidden">
            <div className={`h-full ${r.color}`} style={{ width: `${(r.value / max) * 100}%` }} />
          </div>
        </div>
      ))}
    </div>
  );
}

function RelationshipBlock({ rel }: { rel: Relationship }) {
  return (
    <section>
      <h2 className="font-display text-xl mb-4">Баланс отношений</h2>
      <div className="rounded-xl border border-line bg-card p-5 space-y-5">
        <p className="text-sm">{rel.summary}</p>
        <BalanceBar label="Активность" left={rel.dominantParticipant} right={rel.secondaryParticipant} value={rel.activityBalance} />
        <BalanceBar label="Инициатива" left={rel.dominantParticipant} right={rel.secondaryParticipant} value={rel.initiativeBalance} />
        <BalanceBar label="Ответы" left={rel.dominantParticipant} right={rel.secondaryParticipant} value={rel.responseBalance} />
      </div>
    </section>
  );
}

function BalanceBar({ label, left, right, value }: {
  label: string; left: string; right: string; value: number;
}) {
  return (
    <div>
      <div className="flex justify-between text-xs text-muted mb-1.5">
        <span className="font-mono">{label}</span>
        <span><span className="text-ink">{value}%</span> / {100 - value}%</span>
      </div>
      <div className="flex h-2.5 rounded-full overflow-hidden bg-line">
        <div className="bg-accent" style={{ width: `${value}%` }} />
        <div className="bg-accent/30" style={{ width: `${100 - value}%` }} />
      </div>
      <div className="flex justify-between text-xs mt-1">
        <span className="font-medium truncate max-w-[45%]">{left}</span>
        <span className="text-muted truncate max-w-[45%] text-right">{right}</span>
      </div>
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

function AiButton({ busy, status, onClick, label = "Запустить" }: {
  busy: boolean; status: string; onClick: () => void; label?: string;
}) {
  return (
    <button onClick={onClick} disabled={busy}
      className="text-sm border border-line rounded-md px-3 py-1.5 hover:border-accent transition-colors disabled:opacity-70">
      {busy ? (
        <span className="flex items-center gap-2">
          <span className="w-3 h-3 rounded-full border-2 border-accent border-t-transparent animate-spin" />
          {status}
        </span>
      ) : label}
    </button>
  );
}

function AiInsightBlock({ id }: { id: string }) {
  const [data, setData] = useState<AiInsight | null>(null);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("Считаю…");
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getInsightsAsync(id, (s) => setStatus(statusLabel(s)))); }
    catch (e: any) { setErr(e?.message || "Не удалось получить AI-анализ. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">AI-анализ</h2>
        {!data && <AiButton busy={busy} status={status} onClick={run} />}
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
  const [status, setStatus] = useState("Считаю…");
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getPersonalityAsync(id, (s) => setStatus(statusLabel(s)))); }
    catch (e: any) { setErr(e?.message || "Не удалось построить портреты. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">Портреты участников</h2>
        {!data && <AiButton busy={busy} status={status} onClick={run} />}
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

function LifeTimelineBlock({ id }: { id: string }) {
  const [data, setData] = useState<LifeTimelineResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("Считаю…");
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getLifeTimelineAsync(id, (s) => setStatus(statusLabel(s)))); }
    catch (e: any) { setErr(e?.message || "Не удалось построить хронологию. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">Хронология жизни</h2>
        {!data && <AiButton busy={busy} status={status} onClick={run} label="Построить" />}
      </div>
      {err && <div className="rounded-lg bg-ember/10 text-ember px-4 py-3 text-sm">{err}</div>}
      {data && (
        <div className="rounded-xl border border-line bg-card p-5">
          {data.summary && <p className="text-sm mb-6">{data.summary}</p>}
          {data.events.length === 0 ? (
            <p className="text-sm text-muted">Событий не выделено.</p>
          ) : (
            <ol className="relative border-l-2 border-line ml-2 space-y-6">
              {data.events.map((ev, i) => (
                <li key={i} className="ml-5">
                  <span className="absolute -left-[7px] w-3 h-3 rounded-full bg-accent border-2 border-paper" />
                  <div className="flex items-baseline gap-2 flex-wrap">
                    <span className="font-mono text-xs bg-accent-soft text-accent rounded px-2 py-0.5">{ev.period}</span>
                    <span className="font-display text-base">{ev.title}</span>
                    {ev.category && <span className="font-mono text-[10px] text-muted uppercase tracking-wide">{ev.category}</span>}
                  </div>
                  <p className="text-sm text-muted mt-1">{ev.description}</p>
                </li>
              ))}
            </ol>
          )}
          {data.model && <div className="font-mono text-[11px] text-muted mt-5">{data.model}</div>}
        </div>
      )}
    </section>
  );
}

function EvolutionBlock({ id }: { id: string }) {
  const [data, setData] = useState<PersonalityEvolutionResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("Считаю…");
  const [err, setErr] = useState<string | null>(null);

  async function run() {
    setBusy(true); setErr(null);
    try { setData(await getEvolutionAsync(id, (s) => setStatus(statusLabel(s)))); }
    catch (e: any) { setErr(e?.message || "Не удалось построить эволюцию. Запущена ли Ollama?"); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-xl">Эволюция личности</h2>
        {!data && <AiButton busy={busy} status={status} onClick={run} label="Построить" />}
      </div>
      {err && <div className="rounded-lg bg-ember/10 text-ember px-4 py-3 text-sm">{err}</div>}
      {data && (
        <div className="space-y-4">
          {data.summary && (
            <div className="rounded-xl border border-line bg-accent-soft/50 p-5 text-sm">{data.summary}</div>
          )}
          {data.entries.length === 0 ? (
            <div className="rounded-xl border border-line bg-card p-5 text-sm text-muted">
              Не хватило данных для анализа эволюции.
            </div>
          ) : (
            data.entries.map((e) => (
              <div key={e.participant} className="rounded-xl border border-line bg-card p-5">
                <div className="font-display text-lg mb-3">{e.participant}</div>
                <div className="grid md:grid-cols-2 gap-3 mb-3">
                  <PortraitMini label="Раньше" p={e.before} />
                  <PortraitMini label="Позже" p={e.after} accent />
                </div>
                {e.change && (
                  <p className="text-sm border-t border-line pt-3">
                    <b>Что изменилось.</b> {e.change}
                  </p>
                )}
              </div>
            ))
          )}
          {data.model && <div className="font-mono text-[11px] text-muted">{data.model}</div>}
        </div>
      )}
    </section>
  );
}

function PortraitMini({ label, p, accent }: {
  label: string; p: PersonalityProfile; accent?: boolean;
}) {
  return (
    <div className={`rounded-lg p-4 ${accent ? "bg-accent-soft" : "bg-paper"}`}>
      <div className="font-mono text-[11px] text-muted uppercase tracking-wide mb-2">{label}</div>
      <p className="text-sm mb-2">{p.summary}</p>
      <div className="flex flex-wrap gap-1.5">
        {p.traits.map((t) => (
          <span key={t} className="font-mono text-[11px] bg-card border border-line rounded px-1.5 py-0.5">{t}</span>
        ))}
      </div>
    </div>
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
