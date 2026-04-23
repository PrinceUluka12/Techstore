import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Attach JWT token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Auto-logout on 401
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('access_token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// ── Auth ────────────────────────────────────────────────────────────────────
export const authApi = {
  register: (data) => api.post('/auth/register', data),
  login:    (data) => api.post('/auth/login', data),
  getMe:    ()     => api.get('/auth/me'),
  updateMe: (data) => api.put('/auth/me', data),
}

// ── Products ─────────────────────────────────────────────────────────────────
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
  getByProduct:    (id)      => api.get(`/inventory/product/${id}`),
  lowStock:        ()        => api.get('/inventory/low-stock'),
  adjust:          (id, d)   => api.post(`/inventory/product/${id}/adjust`, d),
  restock:         (id, d)   => api.post(`/inventory/product/${id}/restock`, d),
  updateThreshold: (id, d)   => api.put(`/inventory/product/${id}/threshold`, d),
}

// ── Images ────────────────────────────────────────────────────────────────────
export const imageApi = {
  upload: (formData) => api.post('/uploads/images/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  getAll:  ()         => api.get('/uploads/images'),
  delete:  (fileName) => api.delete(`/uploads/images/${encodeURIComponent(fileName)}`),
}


// ── Reviews ───────────────────────────────────────────────────────────────────
export const reviewApi = {
  getByProduct: (productId, p) => api.get(`/reviews/product/${productId}`, { params: p }),
  getSummary:   (productId)    => api.get(`/reviews/product/${productId}/summary`),
  create:       (data)         => api.post('/reviews', data),
  delete:       (id)           => api.delete(`/reviews/${id}`),
}

// ── Coupons ───────────────────────────────────────────────────────────────────
export const couponApi = {
  validate:  (code, subTotal) => api.post('/coupons/validate', { code, subTotal }),
  getAll:    (p)              => api.get('/coupons', { params: p }),
  getById:   (id)             => api.get(`/coupons/${id}`),
  create:    (data)           => api.post('/coupons', data),
  update:    (id, data)       => api.put(`/coupons/${id}`, data),
  delete:    (id)             => api.delete(`/coupons/${id}`),
}

export const adminApi = {
  dashboard:   ()       => api.get('/admin/dashboard'),
  salesReport: (params) => api.get('/admin/reports/sales', { params }),
  getUsers:    (p)      => api.get('/admin/users', { params: p }),
  toggleUser:  (id)     => api.put(`/admin/users/${id}/toggle`),
}

export default api