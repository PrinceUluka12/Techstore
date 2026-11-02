export const naira = n => `₦${Number(n || 0).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`

// The API's times are UTC but some come without the "Z" that says so
export const utcDate = d => new Date(d.endsWith('Z') ? d : `${d}Z`)
