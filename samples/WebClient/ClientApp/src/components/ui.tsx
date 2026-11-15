import type { ReactNode } from 'react';

export function Layout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-800">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-4xl items-center gap-3 px-4 py-3">
          <a href="/" className="flex items-center gap-2 font-semibold text-slate-900">
            <span className="grid size-7 place-items-center rounded-md bg-emerald-600 text-sm text-white">WC</span>
            Sample web client
          </a>
        </div>
      </header>
      <main className="mx-auto max-w-4xl px-4 py-8">
        <h1 className="mb-6 text-2xl font-semibold text-slate-900">{title}</h1>
        <div className="space-y-6">{children}</div>
      </main>
    </div>
  );
}

export function Card({ children, title, tone }: { children: ReactNode; title?: string; tone?: 'ok' | 'fail' }) {
  const titleStyle = tone === 'ok' ? 'text-emerald-700' : tone === 'fail' ? 'text-red-700' : 'text-slate-900';
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      {title && <h2 className={`mb-4 text-lg font-medium ${titleStyle}`}>{title}</h2>}
      <div className="space-y-4">{children}</div>
    </section>
  );
}

/** A link that looks like a button: the pages navigate with plain links, which the backend answers. */
export function LinkButton({ href, children, variant = 'secondary' }: { href: string; children: ReactNode; variant?: 'primary' | 'secondary' }) {
  const styles = variant === 'primary'
    ? 'bg-emerald-600 text-white hover:bg-emerald-500'
    : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50';
  return (
    <a href={href} className={`inline-flex items-center rounded-lg px-4 py-2 text-sm font-medium transition-colors ${styles}`}>
      {children}
    </a>
  );
}

export function Actions({ children }: { children: ReactNode }) {
  return <div className="flex flex-wrap gap-2">{children}</div>;
}

export function Pre({ children }: { children: ReactNode }) {
  return <pre className="overflow-x-auto rounded-lg bg-slate-900 p-4 font-mono text-xs leading-relaxed text-slate-100">{children}</pre>;
}

export function Muted({ children }: { children: ReactNode }) {
  return <p className="text-sm text-slate-500">{children}</p>;
}

export function Code({ children }: { children: ReactNode }) {
  return <code className="rounded bg-slate-100 px-1 py-0.5 font-mono text-[0.85em] text-slate-800">{children}</code>;
}

export function Link({ href, children }: { href: string; children: ReactNode }) {
  return <a href={href} className="font-medium text-emerald-700 hover:text-emerald-600">{children}</a>;
}
