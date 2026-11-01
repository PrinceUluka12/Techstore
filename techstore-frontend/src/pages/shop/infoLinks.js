// Paths and names of the policy and help pages (InfoPages.jsx). Kept apart from the pages so the
// footer and checkout can link to them without loading their text up front.
// The paths are the old WordPress site's, so links to it (search results, bookmarks) still work.
export const INFO_PAGES = {
  terms:      { path: '/terms-of-service-compliance', label: 'Terms of Service & Compliance' },
  returns:    { path: '/refund-returns-policy',       label: 'Refund and Returns Policy' },
  disclaimer: { path: '/disclaimer',                  label: 'Disclaimer' },
  privacy:    { path: '/privacy-policy',              label: 'Privacy Policy' },
  help:       { path: '/help-support',                label: 'Help & Support' },
}
