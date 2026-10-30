import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Printer, Plus } from 'lucide-react'
import { format } from 'date-fns'
import { posApi } from '../../services/api'
import { Spinner } from '../../components/ui'
import { showApiError } from '../../utils/validation'
import { naira } from './AdminPos'

const METHOD_LABELS = { Cash: 'Cash', Card: 'Card (POS terminal)', BankTransfer: 'Bank transfer', USSD: 'USSD', Other: 'Other' }

// A4 receipt for an in-store sale. No admin sidebar, so it prints on its own;
// the toolbar is hidden when printing (index.css sets the A4 page).
export default function AdminPosReceipt() {
  const { id } = useParams()
  const [sale, setSale]   = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    posApi.getSale(id)
      .then(r => setSale(r.data))
      .catch(err => { setError(err.response?.status === 404 ? 'Sale not found' : 'Could not load this receipt'); showApiError(err, null, 'Could not load this receipt') })
  }, [id])

  if (error) return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-3 text-surface-500">
      <p>{error}</p>
      <Link to="/admin/pos" className="btn-primary">Back to till</Link>
    </div>
  )
  if (!sale) return <div className="min-h-screen flex items-center justify-center"><Spinner size="lg" /></div>

  const { shop } = sale
  const hasCustomer = sale.customerName || sale.customerPhone || sale.customerEmail

  return (
    <div className="min-h-screen bg-surface-100 py-6 px-4 print:bg-white print:p-0">
      <div className="max-w-[210mm] mx-auto mb-4 flex flex-wrap items-center justify-between gap-3 print:hidden">
        <Link to="/admin/pos" className="btn-secondary gap-2"><ArrowLeft className="w-4 h-4" /> Back to till</Link>
        <div className="flex gap-2">
          <Link to="/admin/pos" className="btn-secondary gap-2"><Plus className="w-4 h-4" /> New sale</Link>
          <button type="button" onClick={() => window.print()} className="btn-primary gap-2">
            <Printer className="w-4 h-4" /> Print receipt
          </button>
        </div>
      </div>

      <article className="max-w-[210mm] min-h-[297mm] mx-auto bg-white shadow-sm p-[15mm] text-[13px] text-surface-800 print:shadow-none print:p-0 print:min-h-0">
        {/* Header */}
        <header className="flex items-start justify-between gap-6 pb-6 border-b border-surface-200">
          <div className="flex items-start gap-3">
            <img src="/hytel-logo.png" alt="Hytel Phones" className="w-12 h-12 object-contain" />
            <div>
              <p className="font-display text-lg font-bold text-surface-900">Hytel Phones</p>
              <p className="font-medium">{shop.name}</p>
              {(shop.address || shop.city) && <p className="text-surface-500">{[shop.address, shop.city].filter(Boolean).join(', ')}</p>}
              {shop.phone && <p className="text-surface-500">Tel: {shop.phone}</p>}
            </div>
          </div>
          <div className="text-right">
            <p className="font-display text-xl font-bold tracking-wide text-surface-900">RECEIPT</p>
            <p className="font-mono">{sale.orderNumber}</p>
            <p className="text-surface-500">{format(new Date(sale.createdAt + (sale.createdAt.endsWith('Z') ? '' : 'Z')), 'd MMM yyyy, HH:mm')}</p>
            <p className="text-surface-500">Served by {sale.cashierName}</p>
          </div>
        </header>

        {hasCustomer && (
          <section className="py-4 border-b border-surface-200">
            <p className="text-xs uppercase tracking-wide text-surface-400 mb-1">Customer</p>
            {sale.customerName && <p className="font-medium">{sale.customerName}</p>}
            <p className="text-surface-500">{[sale.customerPhone, sale.customerEmail].filter(Boolean).join(' · ')}</p>
          </section>
        )}

        {/* Items */}
        <table className="w-full mt-4">
          <thead>
            <tr className="text-left text-xs uppercase tracking-wide text-surface-400 border-b border-surface-200">
              <th className="py-2 font-medium">Item</th>
              <th className="py-2 font-medium text-center w-14">Qty</th>
              <th className="py-2 font-medium text-right w-32">Unit price</th>
              <th className="py-2 font-medium text-right w-32">Amount</th>
            </tr>
          </thead>
          <tbody>
            {sale.items.map((item, i) => (
              <tr key={i} className="border-b border-surface-100 align-top">
                <td className="py-2 pr-3">
                  <p className="font-medium text-surface-900">{item.productName}</p>
                  {item.productSKU && <p className="text-xs text-surface-400 font-mono">{item.productSKU}</p>}
                  {item.serialNumbers.length > 0 && (
                    <p className="text-xs text-surface-500 font-mono mt-0.5">
                      IMEI/S/N: {item.serialNumbers.join(', ')}
                    </p>
                  )}
                </td>
                <td className="py-2 text-center">{item.quantity}</td>
                <td className="py-2 text-right">
                  {naira(item.unitPrice)}
                  {item.originalUnitPrice != null && <p className="text-xs text-surface-400 line-through">{naira(item.originalUnitPrice)}</p>}
                </td>
                <td className="py-2 text-right font-medium">{naira(item.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Totals and payments */}
        <section className="mt-4 flex flex-col sm:flex-row print:flex-row justify-between gap-6">
          <div className="sm:w-1/2 print:w-1/2">
            <p className="text-xs uppercase tracking-wide text-surface-400 mb-1">Payment</p>
            <table className="w-full">
              <tbody>
                {sale.payments.map((p, i) => (
                  <tr key={i}>
                    <td className="py-0.5">
                      {METHOD_LABELS[p.method] ?? p.method}
                      {p.reference && <span className="text-surface-400"> · Ref {p.reference}</span>}
                      {p.tendered != null && p.tendered !== p.amount && <span className="text-surface-400"> · tendered {naira(p.tendered)}</span>}
                    </td>
                    <td className="py-0.5 text-right">{naira(p.amount)}</td>
                  </tr>
                ))}
                {sale.changeGiven > 0 && (
                  <tr><td className="py-0.5 text-surface-500">Change given</td><td className="py-0.5 text-right">{naira(sale.changeGiven)}</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <table className="sm:w-2/5 print:w-2/5">
            <tbody>
              <tr><td className="py-0.5 text-surface-500">Subtotal</td><td className="py-0.5 text-right">{naira(sale.subTotal)}</td></tr>
              {sale.discountAmount > 0 && (
                <tr><td className="py-0.5 text-surface-500">Discount</td><td className="py-0.5 text-right">–{naira(sale.discountAmount)}</td></tr>
              )}
              <tr><td className="py-0.5 text-surface-500">VAT (7.5%)</td><td className="py-0.5 text-right">{naira(sale.tax)}</td></tr>
              <tr className="border-t border-surface-300 font-bold text-base">
                <td className="pt-2">Total</td><td className="pt-2 text-right">{naira(sale.total)}</td>
              </tr>
            </tbody>
          </table>
        </section>

        {(sale.discountReason || sale.items.some(i => i.originalUnitPrice != null)) && (
          <p className="mt-4 text-xs text-surface-500">
            Price adjustment: {sale.discountReason ?? '—'}{sale.discountApprovedBy && ` · approved by ${sale.discountApprovedBy}`}
          </p>
        )}
        {sale.notes && <p className="mt-2 text-xs text-surface-500">Note: {sale.notes}</p>}

        <footer className="mt-10 pt-4 border-t border-surface-200 text-center text-xs text-surface-400">
          Thank you for shopping with Hytel Phones. Please keep this receipt.
        </footer>
      </article>
    </div>
  )
}
