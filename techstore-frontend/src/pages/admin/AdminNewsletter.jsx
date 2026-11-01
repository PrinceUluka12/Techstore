import { useEffect, useState } from 'react'
import { Mail, Download, Search } from 'lucide-react'
import { format } from 'date-fns'
import { newsletterApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner, Pagination, EmptyState } from '../../components/ui'
import { showApiError } from '../../utils/validation'

// People who signed up in the shop's footer. The download is for a mailing tool: everyone still
// subscribed, each with their own unsubscribe link to put in the newsletter.
export default function AdminNewsletter() {
  const [data, setData]       = useState(null) // { items, total, page, pageSize, active }
  const [page, setPage]       = useState(1)
  const [search, setSearch]   = useState('')
  const [query, setQuery]     = useState('')
  const [downloading, setDownloading] = useState(false)
  const [failed, setFailed]   = useState(false) // the last load didn't work: offer to try again
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    setFailed(false)
    newsletterApi.list({ page, pageSize: 20, search: query || undefined })
      .then(r => setData(r.data))
      .catch(err => { setFailed(true); showApiError(err, null, 'Failed to load sign-ups') })
  }, [page, query, attempt])

  const download = async () => {
    setDownloading(true)
    try {
      const { data: blob } = await newsletterApi.exportCsv()
      const url = URL.createObjectURL(blob)
      const link = Object.assign(document.createElement('a'),
        { href: url, download: `newsletter-subscribers-${format(new Date(), 'yyyy-MM-dd')}.csv` })
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) { showApiError(err, null, 'Download failed') }
    finally { setDownloading(false) }
  }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[900px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold text-surface-900">Newsletter</h1>
            <p className="text-sm text-surface-500 mt-1">{data ? `${data.active} subscribed` : '…'} · signed up in the shop's footer</p>
          </div>
          <button onClick={download} disabled={downloading || !data?.active} className="btn-primary">
            {downloading ? <Spinner size="sm" /> : <Download className="w-4 h-4" />} Download CSV
          </button>
        </div>

        <p className="text-sm text-surface-500 max-w-2xl">
          The download lists everyone still subscribed, each with their own unsubscribe link. Put that link in
          every newsletter you send; anyone who uses it comes off the list.
        </p>

        {/* attempt too, so searching the same thing again (say, after a failure) really reloads */}
        <form onSubmit={e => { e.preventDefault(); setPage(1); setQuery(search.trim()); setAttempt(a => a + 1) }} className="relative max-w-sm">
          <Search className="w-4 h-4 text-surface-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input className="input pl-9" placeholder="Search by email" value={search} onChange={e => setSearch(e.target.value)} />
        </form>

        {/* Any failed load, first or later (a search, another page): the old list would be the
            wrong answer to what was asked, so it's replaced by the error */}
        {failed ? (
          <div className="card p-8 text-center">
            <p className="text-surface-600">The sign-ups couldn't be loaded.</p>
            <button onClick={() => setAttempt(a => a + 1)} className="btn-secondary mt-4">Try again</button>
          </div>
        ) : !data ? <div className="flex justify-center py-16"><Spinner /></div>
        : data.items.length === 0 ? (
          <EmptyState icon={Mail} title={query ? 'No matches' : 'No sign-ups yet'}
            description={query ? 'No email contains that.' : 'Emails entered in the shop footer will appear here.'} />
        ) : <>
          <div className="table-container">
            <table className="data-table">
              <thead><tr><th>Email</th><th>Signed up</th><th>Status</th></tr></thead>
              <tbody>
                {data.items.map(s => (
                  <tr key={s.id}>
                    <td className="font-medium text-surface-900 break-all">{s.email}</td>
                    <td className="text-surface-500 whitespace-nowrap">{format(new Date(s.subscribedAt), 'MMM d, yyyy')}</td>
                    <td>{s.unsubscribedAt
                      ? <span className="badge-gray">Unsubscribed {format(new Date(s.unsubscribedAt), 'MMM d')}</span>
                      : <span className="badge-green">Subscribed</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} totalPages={Math.ceil(data.total / data.pageSize)} onChange={setPage} />
        </>}
      </div>
    </AdminLayout>
  )
}
