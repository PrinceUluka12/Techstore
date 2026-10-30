import toast from 'react-hot-toast'

// ── Shared form rules for react-hook-form ─────────────────────────────────────
// Keep in sync with TechStore.API/Validators/Validators.cs. Forms using these
// should set `noValidate` so our messages show instead of the browser's bubbles.

export const EMAIL_RE       = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
export const NAME_RE        = /^\p{L}[\p{L}\p{M}\s'.-]*$/u // \p{M}: tone marks in names like Adébáyọ̀
export const NG_PHONE_RE    = /^(\+234|0)[789]\d{9}$/
export const POSTAL_CODE_RE = /^\d{6}$/
export const URL_RE         = /^https?:\/\/\S+$/i

export const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'webp', 'gif'] // ImageService._allowed
export const MAX_IMAGE_BYTES  = 5 * 1024 * 1024                        // ImageService.MaxBytes
export const MAX_UPLOAD_BYTES = 20 * 1024 * 1024                       // ImagesController [RequestSizeLimit]

// Number inputs come through as strings ('' when blank); edit forms may hold real numbers
const isEmpty = v => v === '' || v == null || (typeof v === 'number' && Number.isNaN(v))
const text    = v => (v ?? '').toString().trim()

export const normalizePhone = (v = '') => (v ?? '').replace(/[\s()-]/g, '')

export const maxText = (max) => ({ maxLength: { value: max, message: `Max ${max} characters` } })

// Required text that can't be just spaces
export const requiredText = (label, max) => ({
  validate: { required: v => text(v) !== '' || `${label} is required` },
  ...(max ? maxText(max) : {}),
})

export const emailRules = {
  validate: {
    required: v => text(v) !== '' || 'Email is required',
    format:   v => EMAIL_RE.test(text(v)) || 'Enter a valid email address',
  },
  ...maxText(200),
}

export const nameRules = (label) => ({
  validate: {
    required: v => text(v) !== '' || `${label} is required`,
    letters:  v => NAME_RE.test(text(v)) || 'Letters only',
  },
  ...maxText(50),
})

export const passwordRules = {
  required: 'Password is required',
  validate: {
    length: v => v.length >= 8 || 'At least 8 characters',
    upper:  v => /[A-Z]/.test(v) || 'Include an uppercase letter',
    digit:  v => /[0-9]/.test(v) || 'Include a number',
  },
}

// Optional Nigerian phone; spaces and dashes are fine, normalizePhone strips them
export const phoneRules = {
  validate: v => {
    const phone = normalizePhone(v)
    return phone === '' || NG_PHONE_RE.test(phone) || 'Enter a Nigerian number, e.g. 08012345678'
  },
}

export const postalCodeRules = {
  validate: {
    required: v => text(v) !== '' || 'Postal code is required',
    format:   v => POSTAL_CODE_RE.test(text(v)) || '6 digits, e.g. 100001',
  },
}

export const urlRules = {
  validate: v => text(v) === '' || URL_RE.test(text(v)) || 'Enter a full link starting with https://',
}

export const numberRules = ({ label, required = false, integer = false, positive = false, min, max } = {}) => ({
  validate: {
    required: v => !required || !isEmpty(v) || `${label} is required`,
    number:   v => isEmpty(v) || Number.isFinite(Number(v)) || 'Enter a number',
    integer:  v => isEmpty(v) || !integer || Number.isInteger(Number(v)) || 'Whole numbers only',
    positive: v => isEmpty(v) || !positive || Number(v) > 0 || 'Must be more than 0',
    min:      v => isEmpty(v) || min == null || Number(v) >= min || `Must be at least ${min}`,
    max:      v => isEmpty(v) || max == null || Number(v) <= max || `Must be ${max} or less`,
  },
})

// Adds checks to a rule set that already has a `validate` object
export const withChecks = (rules, checks) => ({ ...rules, validate: { ...rules.validate, ...checks } })

// ── API errors ────────────────────────────────────────────────────────────────
// Field errors from FluentValidation go under the matching input; the toast
// always says why. `fieldMap` maps lowercased API names to form field names
// where they differ (e.g. { shippingpostalcode: 'postalCode' }).
export function showApiError(err, setError, fallback, fieldMap = {}) {
  const { status, data } = err.response ?? {}
  if (status === 429) {
    toast.error('Too many attempts. Please wait a minute and try again.')
    return
  }
  if (status === 413) {
    toast.error('Upload is too large.')
    return
  }
  const fieldErrors = Object.entries(data?.errors ?? {})
  if (setError) {
    fieldErrors.forEach(([key, messages]) => {
      const field = fieldMap[key.toLowerCase()] ?? key.charAt(0).toLowerCase() + key.slice(1)
      setError(field, { type: 'server', message: messages[0] })
    })
  }
  toast.error(data?.message || fieldErrors[0]?.[1]?.[0] || fallback)
}
