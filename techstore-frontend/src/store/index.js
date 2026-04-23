import { create } from 'zustand'
import { persist } from 'zustand/middleware'


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
          },
          token: authResponse.accessToken,
          isAuthenticated: true,
        })
      },

      logout: () => {
        localStorage.removeItem('access_token')
        set({ user: null, token: null, isAuthenticated: false })
      },

      updateUser: (updates) =>
        set(state => ({ user: { ...state.user, ...updates } })),

      isAdmin: () => get().user?.role === 'Admin',
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
