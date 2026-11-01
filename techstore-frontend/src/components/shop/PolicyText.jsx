import Markdown from 'react-markdown'
import { Link } from 'react-router-dom'

// A policy page's text, written in Markdown in Admin -> Policy Pages, in the shop's styles.
// HTML typed into the text is shown as text rather than run, and javascript: links are dropped
// (react-markdown's defaults), so whoever edits the pages can't put code on them.
const components = {
  h1: ({ children }) => <h2 className="font-display text-xl font-semibold text-surface-900 mt-8 mb-2">{children}</h2>,
  h2: ({ children }) => <h2 className="font-display text-lg font-semibold text-surface-900 mt-8 mb-2">{children}</h2>,
  h3: ({ children }) => <h3 className="font-semibold text-surface-900 mt-6 mb-2">{children}</h3>,
  p:  ({ children }) => <p className="mt-3 text-surface-600 leading-relaxed">{children}</p>,
  ul: ({ children }) => <ul className="mt-3 list-disc pl-5 space-y-1.5 text-surface-600 leading-relaxed">{children}</ul>,
  ol: ({ children }) => <ol className="mt-3 list-decimal pl-5 space-y-1.5 text-surface-600 leading-relaxed">{children}</ol>,
  strong: ({ children }) => <strong className="font-semibold text-surface-800">{children}</strong>,
  // Links to the shop's own pages stay in the shop; others (email, WhatsApp, websites) open as
  // usual. A link whose address was dropped (javascript:) is just text, not a link to this page.
  a: ({ href = '', children }) => !href ? <span>{children}</span> : href.startsWith('/')
    ? <Link to={href} className="text-brand-600 hover:underline">{children}</Link>
    : <a href={href} className="text-brand-600 hover:underline"
        {...(/^https?:/.test(href) ? { target: '_blank', rel: 'noopener noreferrer' } : {})}>{children}</a>,
}

export function PolicyText({ children }) {
  return <Markdown components={components}>{children}</Markdown>
}
