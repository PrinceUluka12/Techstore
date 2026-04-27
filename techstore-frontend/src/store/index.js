import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { couponApi, wishlistApi } from '../services/api'


// ── Auth Store ────────────────────────────────────────────────────────────────
export const useAuthStore = create(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      isAuthenticated: false,

      login: (authResponse) => {
        localStorage.setItem('access_token', authResponse.accessToken)
        set({
          user: {
            id: authResponse.userId,
            email: authResponse.email,
            firstName: authResponse.firstName,
            lastName: authResponse.lastName,
            role: authResponse.role,
            permissions: authResponse.permissions ?? [],
          },
          token: authResponse.accessToken,
          isAuthenticated: true,
        })
      },

      // Called after the API logout request is sent (server clears the HttpOnly cookie)
      logout: () => {
        localStorage.removeItem('access_token')
        set({ user: null, token: null, isAuthenticated: false })
      },

      // Used by the silent refresh interceptor to sync the new access token into store state
      updateToken: (newToken) => {
        localStorage.setItem('access_token', newToken)
        set({ token: newToken })
      },

      updateUser: (updates) =>
        set(state => ({ user: { ...state.user, ...updates } })),

      isAdmin: () => get().user?.role === 'Admin',
      hasPermission: (perm) => get().user?.role === 'Admin' || (get().user?.permissions ?? []).includes(perm),
      hasAnyAdminPermission: () => get().user?.role === 'Admin' || (get().user?.permissions ?? []).length > 0,
    }),
    {
      name: 'auth-store',
      partialize: state => ({ user: state.user, token: state.token, isAuthenticated: state.isAuthenticated }),
    }
  )
)

// ── Cart Store ────────────────────────────────────────────────────────────────
export const useCartStore = create((set, get) => ({
  items: [],
  total: 0,
  itemCount: 0,
  cartId: null,
  couponCode: null,
  discountAmount: 0,
  couponLoading: false,
  couponError: null,

  setCart: (cart) => set({
    items: cart.items,
    total: cart.subTotal,
    itemCount: cart.totalItems,
    cartId: cart.id,
  }),

  clearLocal: () => set({
    items: [], total: 0, itemCount: 0, cartId: null,
    couponCode: null, discountAmount: 0, couponError: null,
  }),

  applyCoupon: async (code) => {
    const subTotal = get().total
    set({ couponLoading: true, couponError: null })
    try {
      const { data } = await couponApi.validate(code, subTotal)
      if (data.isValid) {
        set({ couponCode: code, discountAmount: data.discountAmount, couponError: null })
        return { success: true, discountAmount: data.discountAmount }
      } else {
        set({ couponCode: null, discountAmount: 0, couponError: data.errorMessage })
        return { success: false, error: data.errorMessage }
      }
    } catch {
      const msg = 'Failed to validate coupon.'
      set({ couponCode: null, discountAmount: 0, couponError: msg })
      return { success: false, error: msg }
    } finally {
      set({ couponLoading: false })
    }
  },

  removeCoupon: () => set({ couponCode: null, discountAmount: 0, couponError: null }),

  isInCart: (productId) => get().items.some(i => i.productId === productId),
}))

// ── UI Store (sidebar, modals) ────────────────────────────────────────────────
export const useUIStore = create(set => ({
  sidebarOpen: false,
  cartOpen: false,

  toggleSidebar: () => set(s => ({ sidebarOpen: !s.sidebarOpen })),
  toggleCart:    () => set(s => ({ cartOpen: !s.cartOpen })),
  closeCart:     () => set({ cartOpen: false }),
  closeSidebar:  () => set({ sidebarOpen: false }),
}))

// ── Wishlist Store ────────────────────────────────────────────────────────────
export const useWishlistStore = create((set, get) => ({
  items: [],
  loading: false,

  fetch: async () => {
    set({ loading: true })
    try {
      const { data } = await wishlistApi.get()
      set({ items: data })
    } catch { /* ignore — user may not be logged in */ }
    finally { set({ loading: false }) }
  },

  add: async (productId) => {
    try {
      const { data } = await wishlistApi.add(productId)
      set(s => ({ items: s.items.some(i => i.productId === productId) ? s.items : [...s.items, data] }))
      return true
    } catch { return false }
  },

  remove: async (productId) => {
    try {
      await wishlistApi.remove(productId)
      set(s => ({ items: s.items.filter(i => i.productId !== productId) }))
      return true
    } catch { return false }
  },

  toggle: async (productId) => {
    const inList = get().isInWishlist(productId)
    return inList ? get().remove(productId) : get().add(productId)
  },

  isInWishlist: (productId) => get().items.some(i => i.productId === productId),

  clear: () => set({ items: [] }),
}))
