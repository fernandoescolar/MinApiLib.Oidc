import { useEffect, useState, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import type { Antiforgery, Product } from '../spa';

// The UI kit of every page: the flow pages (login, consent...) and the account and administration consoles.

/** A centered page for the sign-in flow. */
export function FlowLayout({ title, product, children, footer }: { title: string; product: Product; children: ReactNode; footer?: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-slate-50 text-slate-800">
      <main className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-4 py-10">
        <div className="mb-6 flex items-center justify-center gap-2 text-slate-900">
          <Logo />
          <span className="text-lg font-semibold">{product.name}</span>
        </div>
        <h1 className="mb-5 text-center text-2xl font-semibold text-slate-900">{title}</h1>
        <div className="space-y-4">{children}</div>
        {footer && <div className="mt-6 text-center text-sm text-slate-500">{footer}</div>}
      </main>
      <footer className="py-4 text-center text-xs text-slate-400">{product.name} · {product.version}</footer>
    </div>
  );
}

export function Logo({ className = 'size-8 text-base' }: { className?: string }) {
  return <span className={`grid place-items-center rounded-lg bg-indigo-600 font-bold text-white ${className}`}>ID</span>;
}

export function Card({ children, title, description, actions, className = '' }: { children: ReactNode; title?: string; description?: string; actions?: ReactNode; className?: string }) {
  return (
    <section className={`rounded-xl border border-slate-200 bg-white p-6 shadow-sm ${className}`}>
      {(title || actions) && (
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            {title && <h2 className="text-lg font-medium text-slate-900">{title}</h2>}
            {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
          </div>
          {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
        </div>
      )}
      <div className="space-y-4">{children}</div>
    </section>
  );
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' | 'ghost'; size?: 'sm' | 'md'; loading?: boolean };

export function Button({ variant = 'primary', size = 'md', loading = false, className = '', children, disabled, ...props }: ButtonProps) {
  const styles = {
    primary: 'bg-indigo-600 text-white hover:bg-indigo-500 disabled:bg-indigo-300',
    secondary: 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:text-slate-400',
    danger: 'border border-red-200 bg-white text-red-700 hover:bg-red-50 disabled:text-red-300',
    ghost: 'text-slate-600 hover:bg-slate-100 disabled:text-slate-400',
  }[variant];
  const sizes = size === 'sm' ? 'px-3 py-1.5 text-xs' : 'px-4 py-2 text-sm';
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors disabled:cursor-not-allowed ${sizes} ${styles} ${className}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading && <Spinner className="size-3.5" />}
      {children}
    </button>
  );
}

export function Spinner({ className = 'size-5' }: { className?: string }) {
  return (
    <svg className={`animate-spin ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  );
}

const inputClass = 'block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200 focus:outline-none disabled:bg-slate-50 disabled:text-slate-500';

export function Field({ label, id, hint, error, className = '', ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; id: string; hint?: string; error?: string }) {
  return (
    <div className={`space-y-1 ${className}`}>
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">{label}</label>
      <input id={id} className={`${inputClass} ${error ? 'border-red-400' : ''}`} aria-invalid={!!error} {...props} />
      {hint && !error && <p className="text-xs text-slate-500">{hint}</p>}
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}

export function TextArea({ label, id, hint, className = '', ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; id: string; hint?: string }) {
  return (
    <div className={`space-y-1 ${className}`}>
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">{label}</label>
      <textarea id={id} className={`${inputClass} min-h-20`} {...props} />
      {hint && <p className="text-xs text-slate-500">{hint}</p>}
    </div>
  );
}

export function Select({ label, id, hint, children, className = '', ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string; id: string; hint?: string }) {
  return (
    <div className={`space-y-1 ${className}`}>
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">{label}</label>
      <select id={id} className={inputClass} {...props}>{children}</select>
      {hint && <p className="text-xs text-slate-500">{hint}</p>}
    </div>
  );
}

export function Checkbox({ label, id, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: ReactNode; id: string; hint?: string }) {
  return (
    <label htmlFor={id} className="flex items-start gap-3 text-sm">
      <input id={id} type="checkbox" className="mt-0.5 size-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" {...props} />
      <span>
        <span className="font-medium text-slate-700">{label}</span>
        {hint && <span className="block text-xs text-slate-500">{hint}</span>}
      </span>
    </label>
  );
}

export function Toggle({ label, checked, onChange, hint, disabled }: { label: ReactNode; checked: boolean; onChange: (value: boolean) => void; hint?: string; disabled?: boolean }) {
  return (
    <label className="flex items-start justify-between gap-4 text-sm">
      <span>
        <span className="font-medium text-slate-700">{label}</span>
        {hint && <span className="block text-xs text-slate-500">{hint}</span>}
      </span>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        className={`relative mt-0.5 inline-flex h-6 w-11 shrink-0 rounded-full transition-colors disabled:opacity-50 ${checked ? 'bg-indigo-600' : 'bg-slate-300'}`}
      >
        <span className={`m-0.5 inline-block size-5 rounded-full bg-white shadow transition-transform ${checked ? 'translate-x-5' : ''}`} />
      </button>
    </label>
  );
}

/** A list of strings (redirect URIs, scopes...): one per line in a textarea, trimmed and de-duplicated. */
export function ListField({ label, id, value, onChange, hint, placeholder }: { label: string; id: string; value: string[]; onChange: (value: string[]) => void; hint?: string; placeholder?: string }) {
  const [text, setText] = useState(value.join('\n'));
  useEffect(() => { setText(value.join('\n')); }, [value]);
  return (
    <TextArea
      label={label}
      id={id}
      hint={hint ?? 'One per line.'}
      placeholder={placeholder}
      value={text}
      onChange={e => setText(e.target.value)}
      onBlur={() => onChange([...new Set(text.split('\n').map(s => s.trim()).filter(Boolean))])}
      spellCheck={false}
    />
  );
}

export function Alert({ kind, children, onClose }: { kind: 'error' | 'success' | 'info' | 'warning'; children: ReactNode; onClose?: () => void }) {
  const styles = {
    error: 'border-red-200 bg-red-50 text-red-800',
    success: 'border-emerald-200 bg-emerald-50 text-emerald-800',
    info: 'border-sky-200 bg-sky-50 text-sky-800',
    warning: 'border-amber-200 bg-amber-50 text-amber-800',
  }[kind];
  return (
    <div role={kind === 'error' ? 'alert' : 'status'} className={`flex items-start justify-between gap-3 rounded-lg border px-4 py-3 text-sm ${styles}`}>
      <div className="min-w-0 flex-1 break-words">{children}</div>
      {onClose && <button type="button" onClick={onClose} className="text-current opacity-60 hover:opacity-100" aria-label="Dismiss">×</button>}
    </div>
  );
}

export function Badge({ children, tone = 'slate' }: { children: ReactNode; tone?: 'slate' | 'green' | 'red' | 'amber' | 'indigo' | 'sky' }) {
  const styles = {
    slate: 'bg-slate-100 text-slate-700',
    green: 'bg-emerald-100 text-emerald-800',
    red: 'bg-red-100 text-red-800',
    amber: 'bg-amber-100 text-amber-800',
    indigo: 'bg-indigo-100 text-indigo-800',
    sky: 'bg-sky-100 text-sky-800',
  }[tone];
  return <span className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ${styles}`}>{children}</span>;
}

export function Muted({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <p className={`text-sm text-slate-500 ${className}`}>{children}</p>;
}

export function Code({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <code className={`rounded bg-slate-100 px-1 py-0.5 font-mono text-[0.85em] text-slate-800 ${className}`}>{children}</code>;
}

export function Link({ href, children, className = '' }: { href: string; children: ReactNode; className?: string }) {
  return <a href={href} className={`font-medium text-indigo-600 hover:text-indigo-500 ${className}`}>{children}</a>;
}

export function AntiforgeryField({ antiforgery }: { antiforgery: Antiforgery }) {
  return <input type="hidden" name={antiforgery.fieldName} value={antiforgery.token} />;
}

/** A divider with a word in the middle ("or"). */
export function Divider({ children }: { children: ReactNode }) {
  return (
    <div className="flex items-center gap-3 text-xs text-slate-400">
      <span className="h-px flex-1 bg-slate-200" />{children}<span className="h-px flex-1 bg-slate-200" />
    </div>
  );
}

export interface Column<T> {
  header: ReactNode;
  cell: (row: T) => ReactNode;
  className?: string;
}

export function Table<T>({ columns, rows, keyOf, empty = 'Nothing here yet.', onRowClick }: { columns: Column<T>[]; rows: T[]; keyOf: (row: T) => string; empty?: ReactNode; onRowClick?: (row: T) => void }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
          <tr>{columns.map((c, i) => <th key={i} className={`py-2 pr-4 font-medium ${c.className ?? ''}`}>{c.header}</th>)}</tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {rows.length === 0 && <tr><td colSpan={columns.length} className="py-6 text-center text-slate-500">{empty}</td></tr>}
          {rows.map(row => (
            <tr key={keyOf(row)} onClick={onRowClick ? () => onRowClick(row) : undefined} className={onRowClick ? 'cursor-pointer hover:bg-slate-50' : ''}>
              {columns.map((c, i) => <td key={i} className={`py-2.5 pr-4 align-top ${c.className ?? ''}`}>{c.cell(row)}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function Pagination({ page, pageSize, total, onChange }: { page: number; pageSize: number; total: number; onChange: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (pages <= 1) return null;
  return (
    <div className="flex items-center justify-between text-sm text-slate-600">
      <span>Page {page} of {pages} · {total} in total</span>
      <div className="flex gap-2">
        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>Previous</Button>
        <Button variant="secondary" size="sm" disabled={page >= pages} onClick={() => onChange(page + 1)}>Next</Button>
      </div>
    </div>
  );
}

export function Tabs({ tabs, active, onChange }: { tabs: { id: string; label: ReactNode }[]; active: string; onChange: (id: string) => void }) {
  return (
    <div className="flex gap-1 overflow-x-auto border-b border-slate-200" role="tablist">
      {tabs.map(tab => (
        <button
          key={tab.id}
          role="tab"
          type="button"
          aria-selected={active === tab.id}
          onClick={() => onChange(tab.id)}
          className={`-mb-px whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium ${active === tab.id ? 'border-indigo-600 text-indigo-700' : 'border-transparent text-slate-500 hover:text-slate-700'}`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}

export function Modal({ title, children, onClose, footer }: { title: string; children: ReactNode; onClose: () => void; footer?: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={onClose}>
      <div role="dialog" aria-modal="true" aria-label={title} className="w-full max-w-lg rounded-xl bg-white p-6 shadow-xl" onClick={e => e.stopPropagation()}>
        <h2 className="mb-4 text-lg font-medium text-slate-900">{title}</h2>
        <div className="space-y-4">{children}</div>
        {footer && <div className="mt-6 flex justify-end gap-2">{footer}</div>}
      </div>
    </div>
  );
}

export function ConfirmDialog({ title, message, confirmLabel = 'Confirm', danger = true, onConfirm, onCancel, busy }: { title: string; message: ReactNode; confirmLabel?: string; danger?: boolean; onConfirm: () => void; onCancel: () => void; busy?: boolean }) {
  return (
    <Modal title={title} onClose={onCancel} footer={<><Button variant="secondary" onClick={onCancel}>Cancel</Button><Button variant={danger ? 'danger' : 'primary'} onClick={onConfirm} loading={busy}>{confirmLabel}</Button></>}>
      <p className="text-sm text-slate-600">{message}</p>
    </Modal>
  );
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 p-8 text-center">
      <p className="font-medium text-slate-700">{title}</p>
      {children && <div className="mt-2 text-sm text-slate-500">{children}</div>}
    </div>
  );
}

export function Stat({ label, value, hint }: { label: string; value: ReactNode; hint?: ReactNode }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="mt-1 text-3xl font-semibold text-slate-900">{value}</p>
      {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
    </div>
  );
}

export function DescriptionList({ items }: { items: { label: string; value: ReactNode }[] }) {
  return (
    <dl className="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
      {items.map(item => (
        <div key={item.label}>
          <dt className="text-xs uppercase tracking-wide text-slate-500">{item.label}</dt>
          <dd className="mt-0.5 break-words text-slate-800">{item.value ?? <span className="text-slate-400">—</span>}</dd>
        </div>
      ))}
    </dl>
  );
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

/** A console: sidebar navigation, header with the user, and the page. */
export function ConsoleLayout({ product, nav, userName, title, children, right }: {
  product: Product;
  nav: ReactNode;
  userName: string;
  title: string;
  children: ReactNode;
  right?: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 lg:flex">
      <aside className={`${open ? 'block' : 'hidden'} w-full border-b border-slate-200 bg-white lg:block lg:w-64 lg:shrink-0 lg:border-r lg:border-b-0`}>
        <div className="flex items-center gap-2 px-5 py-4 font-semibold text-slate-900">
          <Logo className="size-7 text-sm" />
          <span className="truncate">{title}</span>
        </div>
        <nav className="space-y-1 px-3 pb-4">{nav}</nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3 lg:px-8">
          <button type="button" className="rounded-md p-2 text-slate-600 hover:bg-slate-100 lg:hidden" onClick={() => setOpen(!open)} aria-label="Menu">☰</button>
          <span className="truncate text-sm text-slate-500">{product.name}</span>
          <div className="flex items-center gap-3 text-sm">
            {right}
            <span className="hidden font-medium text-slate-700 sm:inline">{userName}</span>
            <form method="get" action="/logout"><Button variant="secondary" size="sm" type="submit">Sign out</Button></form>
          </div>
        </header>
        <main className="flex-1 px-4 py-6 lg:px-8">{children}</main>
        <footer className="px-4 py-3 text-xs text-slate-400 lg:px-8">{product.name} · {product.version}</footer>
      </div>
    </div>
  );
}

export function NavItem({ active, onClick, children, href }: { active: boolean; onClick?: () => void; children: ReactNode; href: string }) {
  return (
    <a
      href={href}
      onClick={e => { if (onClick) { e.preventDefault(); onClick(); } }}
      className={`block rounded-lg px-3 py-2 text-sm font-medium ${active ? 'bg-indigo-50 text-indigo-700' : 'text-slate-600 hover:bg-slate-100'}`}
    >
      {children}
    </a>
  );
}

export function PageHeader({ title, description, actions }: { title: string; description?: ReactNode; actions?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
      <div>
        <h1 className="text-2xl font-semibold text-slate-900">{title}</h1>
        {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

/** Copies a value to the clipboard, with a small confirmation. */
export function CopyButton({ value, label = 'Copy' }: { value: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <Button variant="secondary" size="sm" type="button" onClick={async () => { await navigator.clipboard.writeText(value); setCopied(true); setTimeout(() => setCopied(false), 1500); }}>
      {copied ? 'Copied' : label}
    </Button>
  );
}
