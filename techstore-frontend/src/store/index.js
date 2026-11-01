import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import toast from 'react-hot-toast'
import { cartApi, couponApi, wishlistApi } from '../services/api'


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
// Signed in, the cart is kept on the server and this is the page's copy of it. Signed out, it's
// kept in this browser (GUEST_CART: [{ productId, quantity }]) and priced by the API's
// cart/preview, so it shows what checkout will charge; signing in moves it into the account.
// After a page reload the cart is empty until loadCart has fetched it (loaded = true once it has).
const GUEST_CART = 'guest-cart'
const readGuestCart = () => { try { return JSON.parse(localStorage.getItem(GUEST_CART)) ?? [] } catch { return [] } }
const writeGuestCart = (lines) => {
  try { lines.length ? localStorage.setItem(GUEST_CART, JSON.stringify(lines)) : localStorage.removeItem(GUEST_CART) }
  catch { /* storage blocked: nothing to keep it in */ }
}
const keepGuestCart = (cart) => writeGuestCart(cart.items.map(i => ({ productId: i.productId, quantity: i.quantity })))
const signedIn = () => useAuthStore.getState().isAuthenticated
// Shaped like an API error, so callers show the message the same way
const cartError = (message) => Object.assign(new Error(message), { response: { data: { message } } })
const EMPTY_CART = { items: [], subTotal: 0, totalItems: 0, id: null }

// Changes to the browser's cart run one at a time, each reading the cart the last one saved.
// Otherwise two quick taps on "Add" (each waiting for its price check) would each save a cart
// without the other's item.
let guestCartQueue = Promise.resolve()
const oneAtATime = (change) => {
  const run = guestCartQueue.then(change, change)
  guestCartQueue = run.catch(() => {})
  return run
}

// Goes up each time the cart is emptied (clearLocal: after an order, on sign-out). A change that
// started before then and finishes after (on a slow connection) is dropped, rather than putting
// back the cart that was just ordered.
let guestCartEmptied = 0

export const useCartStore = create((set, get) => ({
  items: [],
  total: 0,
  itemCount: 0,
  cartId: null,
  loaded: false,
  couponCode: null,
  discountAmount: 0,
  couponLoading: false,
  couponError: null,

  setCart: (cart) => set({
    items: cart.items,
    total: cart.subTotal,
    itemCount: cart.totalItems,
    cartId: cart.id,
    loaded: true,
  }),

  loadCart: async () => {
    try {
      if (!signedIn()) await oneAtATime(() => get().priceGuestCart(readGuestCart()))
      else if (readGuestCart().length) await get().mergeGuestCart() // left from a sign-in that couldn't move it all
      else { const { data } = await cartApi.get(); get().setCart(data) }
    }
    catch { set({ loaded: true }) } // offline: treat as empty
  },

  // Prices the browser's cart, keeping only what can still be bought
  priceGuestCart: async (lines) => {
    const emptied = guestCartEmptied
    const cart = lines.length ? (await cartApi.preview(lines)).data : EMPTY_CART
    if (emptied !== guestCartEmptied) return
    keepGuestCart(cart)
    get().setCart(cart)
  },

  // Into the account's cart, or the browser's. Throws an API-style error, e.g. when there isn't enough stock.
  addItem: async (productId, quantity = 1) => {
    if (signedIn()) {
      const { data } = await cartApi.addItem({ productId, quantity })
      get().setCart(data)
      return
    }
    return oneAtATime(async () => {
      const emptied = guestCartEmptied
      const lines = readGuestCart()
      const wanted = (lines.find(l => l.productId === productId)?.quantity ?? 0) + quantity
      const next = lines.some(l => l.productId === productId)
        ? lines.map(l => l.productId === productId ? { productId, quantity: wanted } : l)
        : [...lines, { productId, quantity }]
      const { data } = await cartApi.preview(next)
      const line = data.items.find(i => i.productId === productId)
      if (!line) throw cartError('This product is no longer available.')
      const available = Math.max(line.availableStock ?? 0, 0)
      if (available < wanted) throw cartError(`Only ${available} units available.`)
      if (emptied !== guestCartEmptied) throw cartError('Your cart was just emptied. Please add it again.')
      keepGuestCart(data)
      get().setCart(data)
    })
  },

  removeItem: async (item) => {
    if (signedIn()) { const { data } = await cartApi.removeItem(item.id); get().setCart(data); return }
    await oneAtATime(() => get().priceGuestCart(readGuestCart().filter(l => l.productId !== item.productId)))
  },

  // After signing in, what was put in the cart before joins the account's cart. Anything the
  // shop turns down (sold out or switched off since) is left out. Anything that got no answer,
  // or hit a server fault, stays in this browser marked `retry`, with how many the account had
  // before (`accountHad`), and the next loadCart tries again. The first try may have gone
  // through with only the answer lost, so a retry only adds if the account still has fewer
  // than accountHad + quantity; otherwise it would double it.
  mergeGuestCart: () => oneAtATime(async () => {
    const lines = readGuestCart()
    const keep = []
    try {
      const has = new Map((await cartApi.get()).data.items.map(i => [i.productId, i.quantity]))
      for (const line of lines) {
        const accountHad = line.retry ? line.accountHad : (has.get(line.productId) ?? 0)
        if (line.retry && (has.get(line.productId) ?? 0) >= accountHad + line.quantity) continue // went through last time
        try { await cartApi.addItem({ productId: line.productId, quantity: line.quantity }) }
        catch (err) { if (!err.response || err.response.status >= 500) keep.push({ ...line, retry: true, accountHad }) }
      }
    } catch { keep.push(...lines) } // couldn't read the account's cart: nothing was added, so keep it all
    writeGuestCart(keep)
    if (keep.length) toast.error("Some of your cart couldn't be moved to your account yet. It'll be added next time you open the shop.")
    try { const { data } = await cartApi.get(); get().setCart(data) } catch { set({ loaded: true }) }
  }),

  // The browser's cart, for a guest's checkout
  guestLines: () => readGuestCart(),

  // After an order or on sign-out the cart is known to be empty. Signed in (an account's
  // order), the browser's cart only holds lines still waiting to move into the account, which
  // weren't in that order, so they stay. Signed out (a guest's order, or after signing out) it's
  // cleared, so the next person to sign in on this browser doesn't get them.
  clearLocal: () => {
    guestCartEmptied++
    if (!signedIn()) writeGuestCart([])
    set({
      items: [], total: 0, itemCount: 0, cartId: null, loaded: true,
      couponCode: null, discountAmount: 0, couponError: null,
    })
  },

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
