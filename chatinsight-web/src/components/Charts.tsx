import {
  BarChart, Bar, XAxis, YAxis, ResponsiveContainer, Cell, Tooltip,
  AreaChart, Area, CartesianGrid,
} from "recharts";

const ACCENT = "#5B5BD6";
const LINE = "#E4E4EA";

export function HourChart({ byHour }: { byHour: Record<string, number> }) {
  const data = Array.from({ length: 24 }, (_, h) => ({
    hour: h,
    count: byHour[String(h)] ?? 0,
  }));
  return (
    <ResponsiveContainer width="100%" height={180}>
      <BarChart data={data} margin={{ top: 4, right: 4, bottom: 4, left: -20 }}>
        <XAxis dataKey="hour" tick={{ fontSize: 11, fill: "#6B6B76" }} tickLine={false} axisLine={{ stroke: LINE }} interval={2} />
        <YAxis tick={{ fontSize: 11, fill: "#6B6B76" }} tickLine={false} axisLine={false} width={40} />
        <Tooltip cursor={{ fill: "#ECECFB" }} labelFormatter={(h) => `${h}:00`} />
        <Bar dataKey="count" radius={[3, 3, 0, 0]} fill={ACCENT} />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function AuthorChart({ byAuthor }: { byAuthor: Record<string, number> }) {
  const data = Object.entries(byAuthor)
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 8);
  return (
    <ResponsiveContainer width="100%" height={Math.max(120, data.length * 40)}>
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 16, bottom: 4, left: 8 }}>
        <XAxis type="number" hide />
        <YAxis type="category" dataKey="name" tick={{ fontSize: 12, fill: "#16151A" }} tickLine={false} axisLine={false} width={110} />
        <Tooltip cursor={{ fill: "#ECECFB" }} />
        <Bar dataKey="count" radius={[0, 4, 4, 0]}>
          {data.map((_, i) => (
            <Cell key={i} fill={i === 0 ? ACCENT : "#A9A9E0"} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

export function DayChart({ byDay }: { byDay: Record<string, number> }) {
  const data = Object.entries(byDay)
    .map(([date, count]) => ({ date, count, t: new Date(date).getTime() }))
    .filter((d) => !isNaN(d.t))
    .sort((a, b) => a.t - b.t)
    .map((d) => ({
      date: new Date(d.t).toLocaleDateString("ru-RU", { day: "2-digit", month: "2-digit", year: "2-digit" }),
      count: d.count,
    }));

  if (data.length === 0)
    return <div className="text-sm text-muted">Недостаточно данных по датам.</div>;

  return (
    <ResponsiveContainer width="100%" height={200}>
      <AreaChart data={data} margin={{ top: 4, right: 8, bottom: 4, left: -20 }}>
        <defs>
          <linearGradient id="dayFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={ACCENT} stopOpacity={0.35} />
            <stop offset="100%" stopColor={ACCENT} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke={LINE} vertical={false} />
        <XAxis dataKey="date" tick={{ fontSize: 10, fill: "#6B6B76" }} tickLine={false} axisLine={{ stroke: LINE }}
          interval={Math.max(0, Math.floor(data.length / 8))} minTickGap={20} />
        <YAxis tick={{ fontSize: 11, fill: "#6B6B76" }} tickLine={false} axisLine={false} width={40} />
        <Tooltip cursor={{ stroke: ACCENT }} />
        <Area type="monotone" dataKey="count" stroke={ACCENT} strokeWidth={2} fill="url(#dayFill)" />
      </AreaChart>
    </ResponsiveContainer>
  );
}
