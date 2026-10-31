import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { bankAccountApi } from '../../services/api'

// The accounts customers pay into, as set by admins on the Bank Accounts page.
// null while loading, then a list (empty when none is set up or they couldn't be loaded).
export function useBankAccounts() {
  const [accounts, setAccounts] = useState(null)
  useEffect(() => {
    bankAccountApi.list().then(r => setAccounts(r.data)).catch(() => setAccounts([]))
  }, [])
  return accounts
}

// One box per account, with a button to copy the account number
export function BankAccountList({ accounts }) {
  const [copied, setCopied] = useState(null)

  const copy = (number) => {
    navigator.clipboard.writeText(number)
    setCopied(number)
    toast.success('Account number copied')
    setTimeout(() => setCopied(null), 2000)
  }

  return (
    <div className="space-y-2">
      {accounts.map(a => (
        <div key={`${a.bankName}-${a.accountNumber}`}
          className="flex items-center justify-between gap-3 rounded-xl border border-surface-200 bg-surface-50 px-4 py-3">
          <div className="min-w-0">
            <p className="text-xs text-surface-500">{a.bankName}</p>
            <p className="font-mono text-lg font-semibold tracking-wide text-surface-900">{a.accountNumber}</p>
            <p className="text-sm text-surface-700 truncate">{a.accountName}</p>
          </div>
          <button type="button" onClick={() => copy(a.accountNumber)}
            className={`flex-shrink-0 text-xs px-2.5 py-1 rounded-md transition-colors ${copied === a.accountNumber
              ? 'bg-emerald-100 text-emerald-700' : 'bg-white border border-surface-200 text-surface-600 hover:bg-brand-50 hover:text-brand-600'}`}>
            {copied === a.accountNumber ? '✓ Copied' : 'Copy'}
          </button>
        </div>
      ))}
    </div>
  )
}
