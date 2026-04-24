import { clsx } from 'clsx'
import { X, Loader2 } from 'lucide-react'
import { forwardRef } from 'react'

// ── Spinner ───────────────────────────────────────────────────────────────────
export function Spinner({ size = 'md', className }) {
  const sz = { sm: 'w-4 h-4', md: 'w-5 h-5', lg: 'w-8 h-8', xl: 'w-12 h-12' }[size]
  return <Loader2 className={clsx(sz, 'animate-spin text-brand-500', className)} />
}

// ── Loading page ──────────────────────────────────────────────────────────────
export function LoadingPage() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <Spinner size="xl" />
        <p className="text-surface-400 text-sm">Loading…</p>
      </div>
    </div>
  )
}

// ── Empty State ───────────────────────────────────────────────────────────────
export function EmptyState({ icon: Icon, title, description, action }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-4 text-center">
      {Icon && (
        <div className="w-14 h-14 bg-surface-100 rounded-2xl flex items-center justify-center mb-4">
          <Icon className="w-7 h-7 text-surface-400" />
        </div>
      )}
      <h3 className="font-display font-semibold text-surface-900 text-lg mb-1">{title}</h3>
      {description && <p className="text-surface-500 text-sm max-w-xs">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

// ── Modal ─────────────────────────────────────────────────────────────────────
export function Modal({ open, onClose, title, children, size = 'md' }) {
  if (!open) return null
  const widths = { sm: 'max-w-sm', md: 'max-w-lg', lg: 'max-w-2xl', xl: 'max-w-4xl' }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className={clsx('relative bg-white rounded-2xl shadow-xl w-full animate-fade-up', widths[size])}>
        <div className="flex items-center justify-between p-6 border-b border-surface-100">
          <h2 className="font-display font-semibold text-lg">{title}</h2>
          <button onClick={onClose} className="btn-icon">
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  )
}

// ── Input ─────────────────────────────────────────────────────────────────────
export const Input = forwardRef(({ label, error, className, ...props }, ref) => (
  <div className={className}>
    {label && <label className="label">{label}</label>}
    <input ref={ref} className={clsx('input', error && 'border-red-400 focus:ring-red-400')} {...props} />
    {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
  </div>
))
Input.displayName = 'Input'

// ── Select ────────────────────────────────────────────────────────────────────
export const Select = forwardRef(({ label, error, children, className, ...props }, ref) => (
  <div className={className}>
    {label && <label className="label">{label}</label>}
    <select ref={ref} className={clsx('input appearance-none cursor-pointer', error && 'border-red-400')} {...props}>
      {children}
    </select>
    {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
  </div>
))
Select.displayName = 'Select'

// ── Status Badge ──────────────────────────────────────────────────────────────
const statusMap = {
  Pending:    'badge-yellow',
  Confirmed:  'badge-blue',
  Processing: 'badge-blue',
  Shipped:    'badge-blue',
  Delivered:  'badge-green',
  Cancelled:  'badge-red',
  Refunded:   'badge-gray',
  Paid:       'badge-green',
  Failed:     'badge-red',
}

export function StatusBadge({ status }) {
  return <span className={statusMap[status] || 'badge-gray'}>{status}</span>
}

// ── Price ─────────────────────────────────────────────────────────────────────
export function Price({ value, compare, className }) {
  const fmt = v => `₦${Number(v).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  return (
    <span className={clsx('flex items-baseline gap-2', className)}>
      <span className="font-semibold text-surface-900">{fmt(value)}</span>
      {compare && compare > value && (
        <span className="text-sm text-surface-400 line-through">{fmt(compare)}</span>
      )}
    </span>
  )
}

// ── Pagination ────────────────────────────────────────────────────────────────
export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null
  return (
    <div className="flex items-center gap-2 justify-center mt-8">
      <button onClick={() => onChange(page - 1)} disabled={page === 1} className="btn-secondary btn-sm">← Prev</button>
      <span className="text-sm text-surface-500">Page {page} of {totalPages}</span>
      <button onClick={() => onChange(page + 1)} disabled={page === totalPages} className="btn-secondary btn-sm">Next →</button>
    </div>
  )
}

// ── Stats Card ────────────────────────────────────────────────────────────────
export function StatCard({ label, value, sub, icon: Icon, color = 'brand', trend }) {
  const colors = {
    brand:   'bg-brand-50 text-brand-600',
    green:   'bg-emerald-50 text-emerald-600',
    amber:   'bg-amber-50 text-amber-600',
    red:     'bg-red-50 text-red-600',
  }
  return (
    <div className="stat-card animate-fade-up">
      <div className="flex items-start justify-between">
        <div className={clsx('w-10 h-10 rounded-xl flex items-center justify-center', colors[color])}>
          {Icon && <Icon className="w-5 h-5" />}
        </div>
        {trend !== undefined && (
          <span className={clsx('text-xs font-medium', trend >= 0 ? 'text-emerald-600' : 'text-red-500')}>
            {trend >= 0 ? '+' : ''}{trend}%
          </span>
        )}
      </div>
      <div>
        <p className="text-2xl font-display font-semibold text-surface-900">{value}</p>
        <p className="text-sm text-surface-500 mt-0.5">{label}</p>
        {sub && <p className="text-xs text-surface-400 mt-0.5">{sub}</p>}
      </div>
    </div>
  )
}