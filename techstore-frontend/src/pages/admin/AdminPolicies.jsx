import { useEffect, useState } from 'react'
import { History, ExternalLink } from 'lucide-react'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { policyApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner } from '../../components/ui'
import { PolicyText } from '../../components/shop/PolicyText'
import { INFO_PAGES } from '../shop/infoLinks'
import { showApiError } from '../../utils/validation'

const when = (date) => format(new Date(date), 'd MMM yyyy, h:mm a')

// The shop's policy pages (staff with "Edit Policy Pages"). Every save is kept, so what a page
// said on any date can be checked: the Terms say the version shown when an order was placed is
// the one that applies to it.
export default function AdminPolicies() {
  const [pages, setPages]     = useState(null) // [{ slug, title, body, updatedAt, updatedByName, isDefault }]
  const [failed, setFailed]   = useState(false)
  const [slug, setSlug]       = useState('terms')
  const [draft, setDraft]     = useState('')
  const [history, setHistory] = useState(null) // saved versions of this page, newest first
  const [saving, setSaving]   = useState(false)

  const page = pages?.find(p => p.slug === slug)
  const changed = !!page && draft.trim() !== page.body.trim()

  const load = () => {
    setFailed(false)
    policyApi.list()
      .then(r => setPages(r.data))
      .catch(err => { setFailed(true); showApiError(err, null, 'Failed to load the pages') })
  }
  useEffect(load, [])

  // The chosen page's current text and history; again after a save
  useEffect(() => {
    if (!page) return
    setDraft(page.body)
    setHistory(null)
    policyApi.history(slug).then(r => setHistory(r.data)).catch(() => setHistory([]))
  }, [slug, page?.updatedAt, !!pages])

  const choose = (next) => {
    if (next === slug) return
    if (changed && !window.confirm('Leave this page without saving your changes?')) return
    setSlug(next)
  }

  const save = async () => {
    setSaving(true)
    try {
      const { data } = await policyApi.save(slug, draft)
      setPages(ps => ps.map(p => p.slug === slug ? data : p))
      toast.success('Saved. The shop now shows this version.')
    } catch (err) {
      showApiError(err, null, 'Save failed')
    } finally { setSaving(false) }
  }

  const bringBack = (version) => {
    setDraft(version.body)
    toast.success(`Loaded the version from ${when(version.createdAt)}. Save to make it current again.`)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        <div>
          <h1 className="font-display text-2xl font-semibold text-surface-900">Policy Pages</h1>
          <p className="text-sm text-surface-500 mt-1">
            What customers read in the footer's policy links. Every save is kept below, with who saved it and when.
          </p>
        </div>

        {failed ? (
          <div className="card p-8 text-center">
            <p className="text-surface-600">The pages couldn't be loaded.</p>
            <button onClick={load} className="btn-secondary mt-4">Try again</button>
          </div>
        ) : !pages ? <div className="flex justify-center py-16"><Spinner /></div> : <>
          {/* Which page */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3" role="tablist">
            {pages.map(p => (
              <button key={p.slug} role="tab" aria-selected={p.slug === slug} onClick={() => choose(p.slug)}
                className={`text-left rounded-xl border p-3 transition-colors
                  ${p.slug === slug ? 'border-brand-500 bg-brand-50 ring-1 ring-brand-500' : 'border-surface-200 hover:border-surface-300 bg-white'}`}>
                <span className="block font-medium text-sm text-surface-900">{p.title}</span>
                <span className="block text-xs text-surface-500 mt-0.5">
                  {p.isDefault ? 'Starting text, not edited yet' : `Saved ${when(p.updatedAt)}${p.updatedByName ? ` by ${p.updatedByName}` : ''}`}
                </span>
              </button>
            ))}
          </div>

          {/* Write on the left, see it as customers will on the right */}
          <div className="grid lg:grid-cols-2 gap-4">
            <div className="card p-4 flex flex-col">
              <label htmlFor="policy-text" className="label">Text</label>
              <textarea id="policy-text" value={draft} onChange={e => setDraft(e.target.value)} maxLength={30000}
                className="input font-mono text-[13px] leading-relaxed min-h-[480px] flex-1 resize-y" />
              <p className="text-xs text-surface-400 mt-2 leading-relaxed">
                <code>## Heading</code> · <code>- bullet point</code> · <code>**bold**</code> ·{' '}
                <code>[link text](/help-support)</code> · <code>[email us](mailto:support@hytelphones.ng)</code>.
                A blank line starts a new paragraph.
              </p>
            </div>
            <div className="card p-5 overflow-auto max-h-[640px]">
              <p className="label">Preview</p>
              <h2 className="font-display text-2xl font-semibold text-surface-900">{page?.title}</h2>
              <PolicyText>{draft}</PolicyText>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <button onClick={save} disabled={!changed || saving || !draft.trim()} className="btn-primary">
              {saving ? <Spinner size="sm" /> : 'Save'}
            </button>
            <button onClick={() => setDraft(page.body)} disabled={!changed || saving} className="btn-secondary">Discard changes</button>
            <Link to={INFO_PAGES[slug].path} target="_blank" className="btn-ghost">
              <ExternalLink className="w-4 h-4" /> See it in the shop
            </Link>
            {changed && <span className="text-sm text-amber-700">Not saved yet</span>}
          </div>

          {/* Every saved version of this page */}
          <div className="card p-5">
            <h2 className="font-semibold flex items-center gap-2 mb-3"><History className="w-4 h-4" /> Saved versions</h2>
            {history === null ? <Spinner size="sm" />
            : history.length === 0 ? <p className="text-sm text-surface-500">None yet. The shop shows the starting text until this page is first saved.</p>
            : (
              <ul className="divide-y divide-surface-100">
                {history.map((v, i) => (
                  <li key={v.id} className="py-2.5 flex flex-wrap items-center justify-between gap-2 text-sm">
                    <span>
                      <span className="text-surface-900">{when(v.createdAt)}</span>
                      <span className="text-surface-500"> · {v.createdByName}</span>
                      {i === 0 && <span className="badge-green ml-2">Showing now</span>}
                    </span>
                    {i > 0 && <button onClick={() => bringBack(v)} className="btn-ghost btn-sm">Load into the editor</button>}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>}
      </div>
    </AdminLayout>
  )
}
