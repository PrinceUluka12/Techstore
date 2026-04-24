import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true, // send HttpOnly refresh token cookie on every request
})

// ── Request: attach access token ─────────────────────────────────────────────
api.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// ── Response: silent token refresh on 401 ────────────────────────────────────
let isRefreshing = false
let pendingQueue = [] // requests waiting while refresh is in flight

const processQueue = (error, token = null) => {
  pendingQueue.forEach(({ resolve, reject }) =>
    error ? reject(error) : resolve(token)
  )
  pendingQueue = []
}

api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config

    // Only attempt refresh on 401, once, and not on the refresh call itself
    if (
      err.response?.status === 401 &&
      !original._retry &&
      !original.url?.includes('/auth/refresh') &&
      !original.url?.includes('/auth/login')
    ) {
      if (isRefreshing) {
        // Queue this request until refresh completes
        return new Promise((resolve, reject) => {
          pendingQueue.push({ resolve, reject })
        }).then(token => {
          original.headers.Authorization = `Bearer ${token}`
          return api(original)
        })
      }

      original._retry = true
      isRefreshing = true

      try {
        const { data } = await api.post('/auth/refresh')
        const newToken = data.accessToken
        localStorage.setItem('access_token', newToken)
        api.defaults.headers.common.Authorization = `Bearer ${newToken}`
        original.headers.Authorization = `Bearer ${newToken}`
        processQueue(null, newToken)
        return api(original)
      } catch (refreshErr) {
        processQueue(refreshErr, null)
        localStorage.removeItem('access_token')
        window.location.href = '/login'
        return Promise.reject(refreshErr)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(err)
  }
)

// ── Auth ──────────────────────────────────────────────────────────────────────
export const authApi = {
  register:        (data) => api.post('/auth/register', data),
  login:           (data) => api.post('/auth/login', data),
  refresh:         ()     => api.post('/auth/refresh'),
  logout:          ()     => api.post('/auth/logout'),
  logoutEverywhere:()     => api.post('/auth/logout-everywhere'),
  changePassword:  (data) => api.post('/auth/change-password', data),
  getMe:           ()     => api.get('/auth/me'),
  updateMe:        (data) => api.put('/auth/me', data),
}

// ── Products ──────────────────────────────────────────────────────────────────
export const productApi = {
  search:      (params) => api.get('/products', { params }),
  featured:    ()       => api.get('/products/featured'),
  categories:  ()       => api.get('/products/categories'),
  byCategory:  (id, p)  => api.get(`/products/category/${id}`, { params: p }),
  getById:     (id)     => api.get(`/products/${id}`),
  create:      (data)   => api.post('/products', data),
  update:      (id, d)  => api.put(`/products/${id}`, d),
  delete:      (id)     => api.delete(`/products/${id}`),
}

// ── Cart ──────────────────────────────────────────────────────────────────────
export const cartApi = {
  get:        ()         => api.get('/cart'),
  addItem:    (data)     => api.post('/cart/items', data),
  updateItem: (id, data) => api.put(`/cart/items/${id}`, data),
  removeItem: (id)       => api.delete(`/cart/items/${id}`),
  clear:      ()         => api.delete('/cart'),
}

// ── Orders ────────────────────────────────────────────────────────────────────
export const orderApi = {
  myOrders:     (p)        => api.get('/orders/my', { params: p }),
  getById:      (id)       => api.get(`/orders/${id}`),
  checkout:     (data)     => api.post('/orders/checkout', data),
  getAll:       (p)        => api.get('/orders', { params: p }),
  updateStatus: (id, data) => api.put(`/orders/${id}/status`, data),
  getHistory:   (id)       => api.get(`/orders/${id}/history`),
}

// ── Inventory ─────────────────────────────────────────────────────────────────
export const inventoryApi = {
  getByProduct:    (id)    => api.get(`/inventory/product/${id}`),
  lowStock:        ()      => api.get('/inventory/low-stock'),
  adjust:          (id, d) => api.post(`/inventory/product/${id}/adjust`, d),
  restock:         (id, d) => api.post(`/inventory/product/${id}/restock`, d),
  updateThreshold: (id, d) => api.put(`/inventory/product/${id}/threshold`, d),
}

// ── Images ────────────────────────────────────────────────────────────────────
export const imageApi = {
  upload: (formData) => api.post('/images/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  getAll:  ()         => api.get('/images'),
  delete:  (fileName) => api.delete(`/images/${encodeURIComponent(fileName)}`),
}

// ── Reviews ───────────────────────────────────────────────────────────────────
export const reviewApi = {
  getByProduct: (productId, p) => api.get(`/reviews/product/${productId}`, { params: p }),
  getSummary:   (productId)    => api.get(`/reviews/product/${productId}/summary`),
  create:       (data)         => api.post('/reviews', data),
  delete:       (id)           => api.delete(`/reviews/${id}`),
}

// ── Admin ─────────────────────────────────────────────────────────────────────
export const adminApi = {
  dashboard:     ()       => api.get('/admin/dashboard'),
  salesReport:   (params) => api.get('/admin/reports/sales', { params }),
  getUsers:      (p)      => api.get('/admin/users', { params: p }),
  toggleUser:    (id)     => api.put(`/admin/users/${id}/toggle`),
  promoteToAdmin:(id)     => api.put(`/admin/users/${id}/promote`),
}

export default api
