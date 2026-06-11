import { Link, Outlet, useLocation } from "react-router-dom";

export default function Layout() {
  const { pathname } = useLocation();
  const tab = (to: string, label: string) => {
    const active = pathname === to;
    return (
      <Link
        to={to}
        className={`px-3 py-1.5 text-sm rounded-md transition-colors ${
          active ? "bg-ink text-paper" : "text-muted hover:text-ink"
        }`}
      >
        {label}
      </Link>
    );
  };

  return (
    <div className="min-h-screen">
      <header className="border-b border-line bg-paper/80 backdrop-blur sticky top-0 z-10">
        <div className="mx-auto max-w-5xl px-6 h-16 flex items-center justify-between">
          <Link to="/" className="flex items-baseline gap-2">
            <span className="font-display text-2xl font-semibold tracking-tight">
              ChatInsight
            </span>
            <span className="font-mono text-[11px] text-muted">
              читальня переписок
            </span>
          </Link>
          <nav className="flex items-center gap-1">
            {tab("/", "Чаты")}
            {tab("/upload", "Загрузить")}
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-6 py-10">
        <Outlet />
      </main>
    </div>
  );
}
